# 启动计划与部署快照：影响面与故障定位地图

> 制定日期：2026-07-06
> 定位：启动计划与部署快照的**影响面与故障定位地图**。本文档面向上线后问题定位；Profile 保存基础实例意图，launch/data.plan.json 保存可选的启动层意图，lock 只是可丢弃的计算快照。随代码演进维护：凡改动启动计划或 lock 行为，同步更新本文档。
> 关联：[POLY-116](https://d3ara1n.atlassian.net/browse/POLY-116)
> 当前状态：启动计划基础设施完成，多目录外挂与来源无感叠加待实现

---

## 0. 先理解启动快照（90% 的问题靠这三点定位）

`data.lock.json` 是由 Profile 计算出的可丢弃部署快照，不是用户意图的第二个来源。与当前架构的三个根本特征，理解了就能定位绝大多数异常：

1. **双快照模型** —— 每次部署同时存在 `BaseLock`（上一次计算快照，只读参照）和 `Lock`（本次计算快照，逐步填充）。**各 stage 判断复用条件看 BaseLock、修改写 Lock**，两者解耦；Profile 与外挂启动计划共同构成持久意图。
2. **固定线性 pipeline** —— `DeployEngine.DecideNext` 状态机已废弃，11 个 stage 固定顺序线性执行，各自看 BaseLock 自决干不干活。没有"跳过 stage"的分支了——出问题不要找状态机，找具体 stage 的自决逻辑。
3. **防漂移 = 快照优先** —— 只要上一次快照仍匹配当前 Profile，就复用已解析结果；rule 变化用快照中的 resolved 重算，绝不改变 Profile。任何"版本不对"先想：快照是否仍匹配，或是否需要从 Profile 重新计算。

---

## 1. 责任域地图（按模块）

| 责任域 | 文件 | 职责 | 出错查这里 |
|--------|------|------|-----------|
| **数据结构** | `TridentCore.Abstractions/FileModels/LockData.cs` | lock 的内存与磁盘形态。含 `PlatformData/ViabilityData/LaunchPlanResult/LockedPackage/PackageRule`；`LockedPackage.Resolved` 是**全量 `Package`**、另带 `SuppressedBy`（POLY-117） | 序列化/反序列化异常、字段缺失、跨机器数据不一致 |
| **FastMode 门控** | `TridentCore.Abstractions/Extensions/LockDataExtensions.cs` | `Verify(setup, optionsHash, priorityHash, launchPlanHash)`——FastMode 判定，比 platform + OptionsHash + **PriorityHash** + 外挂 LaunchPlanHash + 完整 pref（含 vid）集合 | "改了 mod 版本/SourceOrders 没生效（FastMode）"、"每次都全量部署" |
| **部署上下文** | `TridentCore.Core/Engines/Deploying/DeployContext.cs` | `BaseLock`/`Lock`/`OptionsHash` 的载体 | stage 间数据传递问题 |
| **pipeline 编排** | `TridentCore.Core/Engines/DeployEngine.cs` | 固定 11-stage 线性序列（包含外挂启动计划加载、解析与 `FlattenPackages`） | stage 执行顺序、stage 未执行（看 Sequence 数组） |
| **加载** | `Stages/LoadLockStage.cs` / `LoadLaunchPlanStage.cs` / `Utilities/LaunchPlanSnapshot.cs` | 在部署开始时读取并校验可选的 `launch/data.plan.json`，LoadLaunchPlanStage 将已捕获的文档接入管线；new Lock 填 Platform/Viability/LaunchPlanHash | lock 或外挂启动计划读不到、结构错误、Platform 填错 |
| **平台缓存·vanilla** | `Stages/InstallVanillaStage.cs` | 看 `BaseLock.Platform==Lock.Platform`→整体迁启动计划；否则重建 vanilla（调 PrismLauncher + authlib-injector） | vanilla 启动缺库、启动计划迁移不完整 |
| **平台缓存·loader** | `Stages/ProcessLoaderStage.cs` | 看 `BaseLock.Platform` 匹配→skip（启动计划已整体迁）；否则重建 loader | loader 启动失败、loader 库缺失、mainClass 错 |
| **版本锁定·核心** | `Stages/SyncPackagesStage.cs` | diff 按 **`(project, Source)`**（POLY-117，同 mod 多来源各自存活）+ floating 失效（platform）+ 固定 vid 变更检测 + 精细 rule（`EvaluateRule` 直接哺 `locked.Resolved`，零网络）| **绝大多数 mod 版本相关问题**（见 §4） |
| **覆盖仲裁**（POLY-117） | `Stages/FlattenPackagesStage.cs` | 两遍 dedupe（project identity + 落盘路径）按 `SourceOrders` 裁决；败者 `SuppressedBy`；同档平手 throw `PackageConflictException` | 同 mod 多来源没裁/裁错、冲突报错、被覆盖的包没落盘 |
| **持久化** | `Stages/PersistLockStage.cs` | 唯一写回磁盘点 | lock 没更新、写文件失败 |
| **下载清单** | `Stages/GenerateManifestStage.cs` | 消费 `Lock.LaunchPlan`+`Lock.Packages`；跳过 `SuppressedBy` 包（POLY-117）；target 由 `locked.RelativeTarget()` 派生（`PackagePathHelper`） | mod 落盘路径错、文件没进 manifest、target 不相对、被覆盖的包不该落盘却落了 |
| **下载执行** | `Stages/SolidifyManifestStage.cs` | 下载 + 软链；**下载后 hash 校验闭环** | 下载失败、hash 不匹配、缓存未命中重复下载 |
| **规则重算** | `Engines/Deploying/PackagePlanner.cs` | `EvaluateRule`（matched 包直接哺全量 `locked.Resolved`，零网络、无假值）/ `ResolveAsync`（`ToLookup` 去重分发，POLY-117）；`PlanAsync` 保留供导出器/宿主 | rule 评估结果错、rule 改了没生效 |
| **库累积** | `TridentCore.Core/Extensions/LockDataExtensions.cs` | `MakeIgniter` + `LibraryHelper`（库 identity 解析与合并规则） | 库重复、库丢失、启动 classpath 错 |
| **启动装配** | `TridentCore.Core/Services/InstanceManager.cs` | FastMode 路径（读 lock→Verify→直接 Launch）；正常路径 foreach 驱动 stage | FastMode 误判、stage 进度跟踪、启动参数装配 |
| **PrismLauncher 适配** | `TridentCore.Core/Services/PrismLauncherService.cs` | `AddValidatedLibraries(LaunchPlan, ...)` | vanilla/loader 库获取 |
| **账户注入** | `Services/AccountConfigurerAgent.cs` + `Accounts/AuthlibAccountConfigurer.cs` | 读 `Lock.LaunchPlan` 装配启动 | 账户/外置登录启动问题 |

---

## 2. 部署 pipeline 各 stage（运行时顺序，按症状定位）

固定序列，**每个都执行**（无跳过），各自内部自决：

```
LoadLock        读 BaseLock + 建 Lock(Platform/Viability/LaunchPlanHash)
   ↓
LoadLaunchPlan  读取可选的 launch/data.plan.json
   ↓
InstallVanilla  BaseLock.Platform 匹配且 LaunchPlan 非空 → 整体迁；否则重建 vanilla
   ↓
ProcessLoader   BaseLock.Platform 匹配且 LaunchPlan 非空 → skip；否则重建 loader
   ↓
ResolveLaunchPlan  将操作序列解析为 Lock.LaunchPlan
   ↓
SyncPackages    按 (project,source) diff → floating/platform 失效重 resolve + 固定 vid 变更重 resolve
                + 精细 rule（EvaluateRule 直接哺 Resolved）→ 组装 Lock.Packages
   ↓
FlattenPackages 两遍仲裁（project identity + 落盘路径）按 SourceOrders 裁决；
                胜者 SuppressedBy=null、败者 SuppressedBy=胜者；同档平手 throw（POLY-117）
   ↓
EnsureRuntime   查 Java 运行时
   ↓
PersistLock     写 data.lock.json（唯一写回）
   ↓
GenerateManifest  从 Lock 生成下载清单（package target 派生）
   ↓
SolidifyManifest  下载 + 软链 + 下载后 hash 校验
```

**关键不变量**：`InstallVanilla` 与 `ProcessLoader` 的判断**都看 `BaseLock.Platform`**（不看 Lock 中间态）。匹配时 InstallVanilla 整体迁、ProcessLoader skip；不匹配时两者各自重建。如果出现"vanilla 迁了但 loader 没迁"或反之，查这两个 stage 的判断是否一致。

---

## 3. 数据流（追踪部署快照去向）

```
Profile + 上一次 data.lock.json
   │ 读（LoadLockStage；无法解析或损坏 → BaseLock=null）
   │ 读（部署入口捕获 plan；不存在则跳过，格式错误则终止部署）
   ▼
BaseLock (上一次快照) ──┐
                        ├─► 各 stage 判断是否可复用
Lock (本次快照) ◄───────┘    各 stage 迁移/重建写入
   │
   │ 写（PersistLockStage，唯一）
   ▼
可丢弃的 data.lock.json

旁路（不进 pipeline）：
磁盘 ──► InstanceManager FastMode ──► Verify ──► 命中直接 Launch / 不命中进 pipeline
```

`LockedPackage`：`Pref` 存**当前声明的完整 pref**（含 vid，diff 键 + Verify 比较单元）；`Source` 是覆盖层身份（POLY-117 仲裁用）；`Resolved` 是**全量 `Package`**（锁定的事实，规则复算/清单/UI 直接读）；`Rule` 是 rule 评估快照；`SuppressedBy`（POLY-117）非 null 表示被该 pref 的包覆盖、不落盘。

---

## 4. 症状 → 定位速查表（核心 debug 价值）

| 症状 | 首查 | 次查 / 说明 |
|------|------|------------|
| **mod 版本漂移到最新** | `SyncPackagesStage` floating 失效判定 | 该走缓存复用却 resolve 了——查 `platformChanged` 是否误判 true，或 purl 的 MatchKey 是否没匹配上 BaseLock |
| **改了固定 mod 版本（@vid）启动还是旧版** | `LockDataExtensions.Verify` | FastMode 用完整 pref 比较；若仍命中旧版，查 Verify 的 pref 集合是否含 vid |
| **rule 改了，mod 行为/路径没变** | `SyncPackagesStage` 精细 rule + `PackagePlanner.EvaluateRule` | matched 包零网络重算，直接哺全量 `locked.Resolved`；查 rule 选择器读的字段是否在 `Package` 上 |
| **每次启动都重新解析（不缓存）** | `Verify`（FastMode）+ `LoadLockStage`（BaseLock 读取） | FastMode 不命中→查 platform/viability/optionsHash/LaunchPlanHash/pref 集合哪个变了；或 BaseLock 读失败 |
| **启动崩溃 / class not found / 主类错** | `InstallVanilla`/`ProcessLoader` 启动计划迁移 + `MakeIgniter` | 启动计划迁移不完整、loader 重建遗漏、Library 合并把需要的库覆盖掉 |
| **mod 文件落盘路径不对** | `PackagePathHelper.RelativeTarget` / `locked.RelativeTarget()` 扩展 | target 由 Rule+Resolved 派生；查 Normalizing/Destination 逻辑 |
| **跨机器复制实例后版本不一致** | `LockData` 可迁移字段 | 应无本地 Key；`Resolved` 是全量 `Package`，含完整复现信息（vid/download/hash 等） |
| **下载的文件损坏未被发现** | `SolidifyManifestStage` hash 校验 | 下载后应有 `FileHelper.VerifyModified` 闭环；查 `Package.Hash` |
| **lock 缺失或不完整后全量重建** | `LoadLockStage` + 平台阶段 | 仅 Profile 作为持久源，lock 无法复用时重新计算启动计划与包锁定 |
| **loader 相关启动失败（Fabric/Forge/Quilt/NeoForge）** | `ProcessLoaderStage` 重建分支 | 仅 platform 不匹配时才重建；查 loader 字符串解析、intermediary（Fabric/Quilt）、ForgeWrapper 参数 |
| **authlib-injector/外置登录失败** | `AccountConfigurerAgent` + `AuthlibAccountConfigurer` | 读 `Lock.LaunchPlan`；查启动计划是否就绪 |
| **加 mod 不解析 / 删 mod 不移除** | `SyncPackagesStage` diff 三桶 | Added/Removed/Matched 分桶；查 MatchKey（project identity 小写）匹配 |
| **同 project 多来源/多版本冲突** | `FlattenPackagesStage`（POLY-117 已实施） | 见 `notes/DeploymentPriority.md`——两遍仲裁 + SourceOrders 覆盖模型 |

---

## 5. 已知复杂点 / 易藏 bug

1. **`SyncPackagesStage.MatchKey`** —— 用 `(Label, Namespace, Pid, Source)` 小写化做 diff 键（POLY-117 加 Source，同 mod 多来源各自存活）。大小写、namespace 缺失（`null`→`""`）处理若变，会导致匹配失败→误判新增/删除→重新 resolve。filter/vid 故意不进 key（支持 fixed→floating 继承）。
2. **floating 失效只看 `platformChanged`** —— 因为 `ResolveAsync` 的 filter 完全从 `setup.Version/Loader` 构建，不从 `entry.Pref` 的 `#filter` 取。**若未来 filter 语义改变**（让 entry 的 filter 参与 resolve），此处必须同步加 entry filter 变更检测，否则 filter 漂移。
3. **固定 vid 变更检测** —— `parsed.Vid != locked.Resolved.VersionId` 触发重 resolve（`Resolved` 现为全量 `Package`，字段名 `VersionId`）。这是对蓝本的补全（尊重用户重定版本）。若用户反馈"锁定太死，改 vid 不生效"，先查这里。
4. **全量 Package 持久化**（POLY-117）—— `LockedPackage.Resolved` 是完整 `Package`（含 Thumbnail/Author/Summary/Reference/Dependencies），规则复算直接哺它、无假值（旧的 `ReconstructPackage` 占位重建已删）。`Reference` 是 Uri 友情链接（≠ `Source` 覆盖身份，二者绝不可混）。结构不匹配的锁不参与计算，直接由 Profile 重新生成。
5. **启动计划整体迁移的原子性** —— InstallVanilla 匹配则整体迁、ProcessLoader 匹配则 skip。两者判断**必须都看 `BaseLock.Platform`**。若任一改成看 Lock 中间态，会出现"半迁移"。
6. **FastMode Verify 多维指纹** —— 比 platform + OptionsHash + PriorityHash + LaunchPlanHash + 完整 pref（含 vid）集合。改 vid 触发重建；改 `SourceOrders`/`Setup.Source` 触发 PriorityHash 变→重建（POLY-117）。若 pref 比较改成 project identity 会重蹈"改 vid 不生效"覆辙。
7. **lock 是可丢弃的计算结果** —— 结构变化直接按当前 Profile 重新生成，不在 lock 上堆叠兼容迁移。
8. **`PackagePlanner.PlanAsync` 保留** —— 导出器（4 个）+ 宿主（`InstancePackageModal`/`InstanceSetupPageModel`）依赖。它内部委托 `ResolveAsync`+`EvaluateRule`，与 pipeline 共享底层，无重复逻辑。若动 ResolveAsync/EvaluateRule 签名，PlanAsync 也要同步。

---

## 6. 降级行为与外部依赖

| 场景 | 行为 | 出错点 |
|------|------|--------|
| `data.lock.json` 不存在 | BaseLock=null，全量构建 | 正常首次部署 |
| lock 缺失、损坏或结构不匹配 | BaseLock 不可复用并重新计算 | `LoadLockStage` 与各 stage 的空值校验 |
| 文件损坏 | 同上 | `LoadLockStage` catch |
| `Options.FullCheckMode=true` | 不读 BaseLock→全量重建 | `LoadLockStage` 顶部条件 |
| PrismLauncher Meta 不可达 | InstallVanilla/ProcessLoader 重建失败→部署中断 | 网络依赖，无降级 |
| 仓库 API（CF/Modrinth）失败 | 仅 SyncPackages 的 toResolve 集合受影响；已锁定的包零网络不受波及 | resolve 失败提示用户 |
| authlib-injector API 失败 | InstallVanilla 重建失败 | 部署中断 |

---

## 7. 改动文件全清单（按责任域，实施完成态）

**新增**
- `TridentCore.Abstractions/FileModels/LaunchPlanResult.cs`
- `TridentCore.Abstractions/LaunchPlans/LaunchPlan.cs`
- `TridentCore.Abstractions/LaunchPlans/LaunchPlanDocument.cs`
- `TridentCore.Abstractions/Utilities/LibraryHelper.cs`
- `TridentCore.Core/Engines/Deploying/Stages/LoadLaunchPlanStage.cs`
- `TridentCore.Core/Engines/Deploying/Stages/ResolveLaunchPlanStage.cs`
- `TridentCore.Core/Utilities/LaunchPlanSnapshot.cs`

**删除**
- 旧版构建阶段、LockDataBuilder 和构建异常类型

**重写/重大修改**
- `Abstractions/FileModels/LockData.cs` / `Abstractions/PathDef.cs` / `Abstractions/Importers/ImportedProfileContainer.cs`（结构与实例路径）
- `Abstractions/Extensions/LockDataExtensions.cs`（Verify 重写）
- `Core/Engines/Deploying/DeployContext.cs` / `DeployEngine.cs` / `DeployStage.cs`
- `Core/Engines/Deploying/Stages/InstallVanillaStage.cs` / `ProcessLoaderStage.cs` / `GenerateManifestStage.cs` / `SolidifyManifestStage.cs` / `EnsureRuntimeStage.cs`
- `Core/Engines/Deploying/PackagePlanner.cs`（拆 EvaluateRule/ResolveAsync）
- `Core/Extensions/LockDataExtensions.cs` / `Core/Utilities/ViabilityHashHelper.cs`（启动装配与快照指纹）
- `Core/Services/InstanceManager.cs` / `PrismLauncherService.cs` / `AccountConfigurerAgent.cs`
- `Accounts/AuthlibAccountConfigurer.cs` / `Core/Services/AccountConfigurerAgent.cs`
- `DeployStage.cs`（枚举改名）
- `Resources.resx` / `Resources.zh-hans.resx`（部署阶段资源键）
- `Core/Services/SnapshotManager.cs` / `Core/Services/ImporterAgent.cs` / `Core/Services/InstanceManager.cs` / `Core/Exporters/TridentExporter.cs` / `Core/Importers/TridentImporter.cs`（外挂目录生命周期）

**宿主（src/Polymerium.Avalonia）**
- `PageModels/InstanceHomePageModel.cs`（stage→resource 映射）
- `Services/Instances/DeployTracker.cs`（默认 stage）
- 经 grep 确认无其他旧版启动计划机制残留

---

## 8. 维护约定

- 凡改动 lock 的**结构**（新增/改字段）：更新本文件 §1 数据结构行 + §5 相关易错点。
- 凡改动 **pipeline stage** 顺序或职责：更新 §2 + §1 对应行。
- 凡改动 **SyncPackages** 的 diff/失效/rule 逻辑：重点更新 §4 症状表 + §5 对应条目（这是 bug 高发区）。
- 凡改动 **Verify**：更新 §4 的 FastMode 相关行。
- 新增 **rule 选择器类型**：现在直接读全量 `Package`（`locked.Resolved`），无重建/无假值——只需确认该字段在 `Package` 上（§5.4）。
- 凡改动 **来源覆盖仲裁 / SourceOrders / FlattenPackages / SuppressedBy**：见 `notes/DeploymentPriority.md`（POLY-117 专属 note）。
