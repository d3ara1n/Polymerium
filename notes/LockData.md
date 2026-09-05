# 启动计划与部署快照：影响面与故障定位地图

> 制定日期：2026-07-06
> 定位：启动计划与部署快照的**影响面与故障定位地图**。本文档面向上线后问题定位；Profile 保存基础实例意图，launch/source/ 与 launch/ 根目录下的原生 plan 文件保存可选的启动层意图，lock 只是可丢弃的计算快照。随代码演进维护：凡改动启动计划或 lock 行为，同步更新本文档。
> 关联：[POLY-116](https://d3ara1n.atlassian.net/browse/POLY-116)、[POLY-165](https://d3ara1n.atlassian.net/browse/POLY-165)
> 当前状态：启动计划已收敛为「层 + 折叠」单一模型，MMC patch 转换与诊断漏斗已实现；不支持计划的导出格式提示和 Portable Instance 支持待第三阶段

---

## 0. 先理解启动快照（90% 的问题靠这三点定位）

`data.lock.json` 是由 Profile 计算出的可丢弃部署快照，不是用户意图的第二个来源。与当前架构的三个根本特征，理解了就能定位绝大多数异常：

1. **双快照模型** —— 每次部署同时存在 `BaseLock`（上一次计算快照，只读参照）和 `Lock`（本次计算快照，逐步填充）。**各 stage 判断复用条件看 BaseLock、修改写 Lock**，两者解耦；Profile 与外挂启动计划共同构成持久意图。
2. **固定线性 pipeline** —— `DeployEngine.DecideNext` 状态机已废弃，10 个 stage 固定顺序线性执行，各自看 BaseLock 自决干不干活。没有"跳过 stage"的分支了——出问题不要找状态机，找具体 stage 的自决逻辑。
3. **防漂移 = 快照优先** —— 只要上一次快照仍匹配当前 Profile，就复用已解析结果；rule 变化用快照中的 resolved 重算，绝不改变 Profile。任何"版本不对"先想：快照是否仍匹配，或是否需要从 Profile 重新计算。

启动计划自身还有第四点，它解释了绝大多数「设置改了却没生效」类反馈：

4. **层 + 折叠是唯一模型** —— vanilla、loader、`launch/source/` 托管层、`launch/` 用户层**同质**，都是一段有序 operation（`LaunchLayer`）；`LaunchPlanFolder.Fold` 按序折叠成 `LaunchPlanResult`。**后叠加者覆盖先前声明就是叠加的定义**，折叠不阻止覆盖，只把跨层覆盖记录为 `LaunchPlanOverride` 并由 `ResolveLaunchPlanStage` 打成 warning 日志。用户改了 Profile 版本却被来源层顶回旧值时，那几条日志是唯一的解释来源。

---

## 1. 责任域地图（按模块）

| 责任域 | 文件 | 职责 | 出错查这里 |
|--------|------|------|-----------|
| **数据结构** | `TridentCore.Abstractions/FileModels/LockData.cs` | lock 的内存与磁盘形态。含 `PlatformData/ViabilityData/LaunchPlanResult/LockedPackage/PackageRule`；`LockedPackage.Resolved` 是**全量 `Package`**、另带 `SuppressedBy`（POLY-117） | 序列化/反序列化异常、字段缺失、跨机器数据不一致 |
| **启动计划模型** | `Abstractions/LaunchPlans/`（`LaunchLayer` / `LaunchPlanDocument` / `LaunchPlanFolder` / `LaunchPlanValidator` / `LaunchPlanOverride` / `LaunchPlanResult`） | 层的表示、operation 静态校验、折叠与跨层覆盖记录。**没有 builder**——校验是 operation 序列的性质，不需先造累加器 | 计划文件被判非法、折叠结果不符预期、覆盖未被报告 |
| **部署上下文** | `TridentCore.Core/Engines/Deploying/DeployContext.cs` | `BaseLock`/`Lock`/`PlatformLayers`/`LaunchPlanSnapshot` 的载体；`CanReuseLaunchPlan` 判平台层能否复用 | stage 间数据传递问题 |
| **pipeline 编排** | `TridentCore.Core/Engines/DeployEngine.cs` | 固定 10-stage 线性序列 | stage 执行顺序、stage 未执行（看 SEQUENCE 数组） |
| **启动计划读取** | `Core/Utilities/LaunchPlanSnapshot.cs` | 扫 `launch/source/` 与 `launch/` 根的直接 plan 文件（点号前缀忽略），解析本地相对引用为绝对 `file://`，并对**整棵树**算内容指纹 | 计划读不到、JSON 错、引用越界或文件缺失、改了文件却复用旧产物 |
| **加载** | `Stages/LoadLockStage.cs` | 读磁盘锁为 BaseLock；new Lock 填 Platform/Viability(LaunchPlanHash) | lock 读不到、Platform 填错 |
| **平台层·vanilla** | `Stages/InstallVanillaStage.cs` | `CanReuseLaunchPlan` 命中→不产层；否则构造 vanilla 层追加进 `Context.PlatformLayers`（调 PrismLauncher + authlib-injector） | vanilla 启动缺库、层未产出 |
| **平台层·loader** | `Stages/ProcessLoaderStage.cs` | 同上判断；否则构造 loader 层。ForgeWrapper 参数查**已产出层 + 本层待定 operation**的最后一次声明，与折叠仲裁一致 | loader 启动失败、loader 库缺失、mainClass 错 |
| **计划折叠** | `Stages/ResolveLaunchPlanStage.cs` | 把平台层 + 快照层按序折叠写入 `Lock.LaunchPlan`；**全流程唯一写入点**；跨层覆盖打 warning 日志 | 启动参数/主类/库不符预期、覆盖未被解释 |
| **版本锁定·核心** | `Stages/SyncPackagesStage.cs` | diff 按 **`(project, Source)`**（POLY-117，同 mod 多来源各自存活）+ floating 失效（platform）+ 固定 vid 变更检测 + 精细 rule（`EvaluateRule` 直接哺 `locked.Resolved`，零网络）| **绝大多数 mod 版本相关问题**（见 §4） |
| **覆盖仲裁**（POLY-117） | `Stages/FlattenPackagesStage.cs` | 两遍 dedupe（project identity + 落盘路径）按 `SourceOrders` 裁决；败者 `SuppressedBy`；同档平手 throw `PackageConflictException` | 同 mod 多来源没裁/裁错、冲突报错、被覆盖的包没落盘 |
| **持久化** | `Stages/PersistLockStage.cs` | 唯一写回磁盘点 | lock 没更新、写文件失败 |
| **下载清单** | `Stages/GenerateManifestStage.cs` | 消费 `Lock.LaunchPlan`+`Lock.Packages`；跳过 `SuppressedBy` 包（POLY-117）；target 由 `locked.RelativeTarget()` 派生；库落点由 `LibraryLocation.Of` 派生 | mod 落盘路径错、文件没进 manifest、本地库被当成下载项 |
| **下载执行** | `Stages/SolidifyManifestStage.cs` | 下载 + 软链；**下载后 hash 校验闭环** | 下载失败、hash 不匹配、缓存未命中重复下载 |
| **规则重算** | `Engines/Deploying/PackagePlanner.cs` | `EvaluateRule`（matched 包直接哺全量 `locked.Resolved`，零网络、无假值）/ `ResolveAsync`（`ToLookup` 去重分发，POLY-117）；`PlanAsync` 保留供导出器/宿主 | rule 评估结果错、rule 改了没生效 |
| **库仲裁** | `Abstractions/Utilities/LibraryHelper.cs` | `Merge` 两条有序子句：角色优先（IsPresent=true 胜）→ 同角色后来者胜；返回被顶替者供报告。**就地替换**保住 classpath 位置 | 库重复/丢失、classpath 顺序变了、同坐标双版本 |
| **库落点** | `Abstractions/Utilities/LibraryLocation.cs` | 远程库→共享缓存；`file://` 本地库→**原地消费**，不进共享缓存 | 本地库找不到、整合包污染其他实例的共享缓存 |
| **启动装配** | `Core/Extensions/LaunchPlanResultExtensions.cs` + `Core/Services/InstanceManager.cs` | `MakeIgniter` 组装 igniter（库路径走 `LibraryLocation`）；InstanceManager foreach 驱动 stage | 启动参数装配、stage 进度跟踪 |
| **PrismLauncher 适配** | `TridentCore.Core/Services/PrismLauncherService.cs` | `AddValidatedLibraries(ICollection<Operation>, ...)`——向 operation 列表追加库 | vanilla/loader 库获取 |
| **格式转换诊断** | `Core/Services/ImporterAgent.cs` + `ExporterAgent.cs` | 诊断唯一出口：导入/导出产生的 `LaunchPlanDiagnostic` 必落日志（Error/Warning 分级），宿主另行弹通知 | 整合包内容静默丢失、用户不知道导入不完整 |
| **账户注入** | `Services/AccountConfigurerAgent.cs` + `Accounts/AuthlibAccountConfigurer.cs` | 读 `Lock.LaunchPlan` 装配启动 | 账户/外置登录启动问题 |

---

## 2. 部署 pipeline 各 stage（运行时顺序，按症状定位）

固定序列，**每个都执行**（无跳过），各自内部自决：

```
LoadLock        读 BaseLock + 建 Lock(Platform/Viability/LaunchPlanHash)
   ↓
InstallVanilla  CanReuseLaunchPlan 命中 → 不产层；否则构造 vanilla 层进 PlatformLayers
   ↓
ProcessLoader  同上判断；否则构造 loader 层
   ↓
ResolveLaunchPlan  折叠平台层 + 快照层 → Lock.LaunchPlan（唯一写入点；跨层覆盖打 warning）
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

**关键不变量**：`InstallVanilla` 与 `ProcessLoader` 的判断**都只看 `CanReuseLaunchPlan`**（即 `BaseLock.Platform==Lock.Platform && LaunchPlan 非空 && LaunchPlanHash 匹配`）。命中时两者都不产层、折叠用上一次结果当基底层；不命中时各自重建。如果出现"vanilla 层产了但 loader 层没产"或反之，查这两个 stage 的判断是否一致，以及 `CanReuseLaunchPlan` 的三个条件是否没同时满足。

---

## 3. 数据流（追踪部署快照去向）

```
Profile + launch/ 计划层 + 上一次 data.lock.json
   │ 读（LoadLockStage；无法解析或损坏 → BaseLock=null）
   │ 读（部署入口捕获 LaunchPlanSnapshot；不存在则跳过，格式错误则终止部署）
   ▼
BaseLock (上一次快照) ──┐
                        ├─► 各 stage 判断是否可复用
Lock (本次快照) ◄───────┘    各 stage 迁移/重建写入
   │
   │ 写（PersistLockStage，唯一）
   ▼
可丢弃的 data.lock.json
```

`LockedPackage`：`Pref` 存**当前声明的完整 pref**（含 vid，diff 键 + Verify 比较单元）；`Source` 是覆盖层身份（POLY-117 仲裁用）；`Resolved` 是**全量 `Package`**（锁定的事实，规则复算/清单/UI 直接读）；`Rule` 是 rule 评估快照；`SuppressedBy`（POLY-117）非 null 表示被该 pref 的包覆盖、不落盘。

---

## 4. 症状 → 定位速查表（核心 debug 价值）

| 症状 | 首查 | 次查 / 说明 |
|------|------|------------|
| **mod 版本漂移到最新** | `SyncPackagesStage` floating 失效判定 | 该走缓存复用却 resolve 了——查 `platformChanged` 是否误判 true，或 purl 的 MatchKey 是否没匹配上 BaseLock |
| **改了固定 mod 版本（@vid）启动还是旧版** | `SyncPackagesStage` 固定 vid 变更检测 | `parsed.Vid != locked.Resolved.VersionId` 应触发重 resolve；查该判断与 MatchKey |
| **rule 改了，mod 行为/路径没变** | `SyncPackagesStage` 精细 rule + `PackagePlanner.EvaluateRule` | matched 包零网络重算，直接哺全量 `locked.Resolved`；查 rule 选择器读的字段是否在 `Package` 上 |
| **每次部署都重建启动计划** | `DeployContext.CanReuseLaunchPlan` | 三个条件任一不满即重建：platform 变、BaseLock.LaunchPlan 为空、LaunchPlanHash 不等。指纹覆盖 `launch/` 整棵树，**动任何一个文件（含被禁用的）都会失效**——有意做粗 |
| **改了 Profile 版本/加载器却没生效** | `ResolveLaunchPlanStage` 的 warning 日志 | 来源层或用户层正在覆盖平台层的声明（asset index / Java 版本 / 库版本）。**这是叠加语义，不是 bug**；要止住则给该计划文件加点号前缀或删除它 |
| **启动崩溃 / class not found / 主类错** | `ResolveLaunchPlanStage` 折叠结果 + `MakeIgniter` | 层未产出、跨层覆盖把需要的库顶掉（看 warning 日志）、`LibraryHelper.Merge` 仲裁选错、classpath 顺序变了 |
| **mod 文件落盘路径不对** | `PackagePathHelper.RelativeTarget` / `locked.RelativeTarget()` 扩展 | target 由 Rule+Resolved 派生；查 Normalizing/Destination 逻辑 |
| **整合包导入后启动行为与原包不同** | `ImporterAgent` 的诊断日志 | MMC 的 jarMods（1.7.10 客户端 jar 重打包）不支持，报 Error；traits/agents 报 Warning。这些内容无法转为原生 operation |
| **本地库/asset index 找不到** | `LibraryLocation.Of` + `LaunchPlanSnapshot.ResolveUri` | `file://` 库原地消费（不进共享缓存、不进 manifest）；托管层引用必须留在 `launch/` 内，用户层可用绝对路径 |
| **跨机器复制实例后版本不一致** | `LockData` 可迁移字段 | 应无本地 Key；`Resolved` 是全量 `Package`，含完整复现信息（vid/download/hash 等）。但用户层计划的绝对路径引用不可迁移 |
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
5. **平台层复用的原子性** —— InstallVanilla 与 ProcessLoader **必须看同一个 `CanReuseLaunchPlan`**。若任一改成自己算一套条件，会出现「只有 vanilla 层、没有 loader 层」的半成品折叠。
6. **折叠不阻止覆盖，只报告** —— 后叠加者胜是叠加的定义，不要为了「保护 Profile」去限制来源层能改什么——那会把 patch 的语义打断。平台层内部的相互覆盖（Forge 重写 args/mainClass）**不**进报告，否则真正需要用户知晓的外部层覆盖会被淹没。
7. **库仲裁必须由恒定规则消化** —— vanilla+Fabric 天生大量同坐标碰撞（asm、guava 各自带版本）。不要改成依赖外部 plan 写 `remove-libraries`：那要求整合包为每次内在碰撞写一条移除，漏写即产生同坐标双版本。
8. **`Merge` 就地替换而非重插** —— 库顺序即 classpath 顺序，把赢家移到队尾会静默改变同名类的加载优先级。
9. **`launch/` 指纹有意做粗** —— 整棵树按路径+内容，而非只跟踪被引用的文件。代价是偶尔多一次重建，换来的是「改了文件却复用旧产物」在结构上不可能发生。`HashHelper.ComputeObjectHash` 开了 `IncludeFields`，使传入只含字段的对象（如 ValueTuple）不再静默退化成 `{}`。
10. **lock 是可丢弃的计算结果** —— 结构变化直接按当前 Profile 重新生成，不在 lock 上堆叠兼容迁移。
11. **`PackagePlanner.PlanAsync` 保留** —— 导出器（4 个）+ 宿主（`InstancePackageModal`/`InstanceSetupPageModel`）依赖。它内部委托 `ResolveAsync`+`EvaluateRule`，与 pipeline 共享底层，无重复逻辑。若动 ResolveAsync/EvaluateRule 签名，PlanAsync 也要同步。

---

## 6. 降级行为与外部依赖

| 场景 | 行为 | 出错点 |
|------|------|--------|
| `data.lock.json` 不存在 | BaseLock=null，全量构建 | 正常首次部署 |
| lock 缺失、损坏或结构不匹配 | BaseLock 不可复用并重新计算 | `LoadLockStage` 与各 stage 的空值校验 |
| 文件损坏 | 同上 | `LoadLockStage` catch |
| `Options.FullCheckMode=true` | 不读 BaseLock→全量重建 | `LoadLockStage` 顶部条件 |
| `launch/` 不存在或无 plan 文件 | 快照为 null，仅平台层参与折叠 | 正常（启动计划是可选的） |
| plan 文件 JSON 错、operation 非法、引用文件缺失或越界 | 抛异常→**部署失败** | `LaunchPlanSnapshot.Load` / `ResolveUri`；用户需修正或给文件加点号前缀禁用 |
| MMC patch 含 jarMods/traits/agents | 转换继续，但发诊断（jarMods 为 Error） | `MultiMcPatchConverter`；诊断经 `ImporterAgent` 落日志并弹通知 |
| PrismLauncher Meta 不可达 | InstallVanilla/ProcessLoader 重建失败→部署中断 | 网络依赖，无降级 |
| 仓库 API（CF/Modrinth）失败 | 仅 SyncPackages 的 toResolve 集合受影响；已锁定的包零网络不受波及 | resolve 失败提示用户 |
| authlib-injector API 失败 | InstallVanilla 重建失败 | 部署中断 |

---

## 7. 关键文件清单（现状）

**启动计划模型**（`TridentCore.Abstractions/LaunchPlans/`）
- `LaunchLayer.cs` —— 层的表示（有序 operation + `Origin` 判别式：Platform/Managed/User）
- `LaunchPlanDocument.cs` —— 磁盘 schema 与 operation 多态定义
- `LaunchPlanValidator.cs` —— operation 序列的静态校验
- `LaunchPlanFolder.cs` —— 唯一的折叠入口，产出 `LaunchPlanResult` + 覆盖记录
- `LaunchPlanOverride.cs` / `LaunchPlanDiagnostic.cs` / `LaunchPlanResult.cs`

**仲裁与落点**（`TridentCore.Abstractions/Utilities/`）
- `LibraryHelper.cs` —— identity 解析 + `Merge` 仲裁（两条有序子句）
- `LibraryLocation.cs` —— 库落点推导（远程→缓存，`file://`→原地）

**管线**（`TridentCore.Core/Engines/Deploying/`）
- `DeployContext.cs`（`PlatformLayers` / `CanReuseLaunchPlan`）、`DeployStage.cs`、`../DeployEngine.cs`
- `Stages/InstallVanillaStage.cs` / `ProcessLoaderStage.cs` —— 平台层生产者
- `Stages/ResolveLaunchPlanStage.cs` —— 折叠与覆盖报告
- `Stages/GenerateManifestStage.cs` / `SolidifyManifestStage.cs` —— 消费折叠结果

**文件与格式边界**（`TridentCore.Core/`）
- `Utilities/LaunchPlanSnapshot.cs` —— `launch/` 读取、引用解析、树指纹
- `Utilities/LaunchPlanFileHelper.cs` —— 文件名约定、UID 生成、路径常量
- `Utilities/MultiMcPatchConverter.cs` / `Models/MultiMcPack/MmcPatch.cs` —— MMC patch ↔ 原生计划
- `Importers/MultiMcImporter.cs` / `TridentImporter.cs`、`Exporters/MultiMcExporter.cs` / `TridentExporter.cs`
- `Services/ImporterAgent.cs` / `ExporterAgent.cs` —— 诊断唯一出口
- `Services/InstanceManager.cs` —— stage 驱动、启动装配、`UpdateAsync` 的 `DirectorySwap` 提交/回滚
- `Services/Instances/DirectorySwap.cs` —— 目录级原子替换的三元组
- `Extensions/LaunchPlanResultExtensions.cs`（`MakeIgniter`）/ `LockedPackageExtensions.cs`（`RelativeTarget`）

**宿主（src/Polymerium.Avalonia）**
- `PageModels/InstanceHomePageModel.cs`（stage→resource 映射）、`Services/Instances/DeployTracker.cs`（默认 stage）
- `PageModels/NewInstancePageModel.cs`（导入诊断通知）

---

## 8. 维护约定

- 凡改动 lock 的**结构**（新增/改字段）：更新本文件 §1 数据结构行 + §5 相关易错点。
- 凡改动 **pipeline stage** 顺序或职责：更新 §2 + §1 对应行。
- 凡改动 **launch/ 下的文件发现、排序、相对引用或树指纹**：更新 §1、§2、§5、§6 相关条目。
- 凡改动 **折叠语义或覆盖报告口径**：更新 §0.4 + §5.6，并确认 §4「改了 Profile 却没生效」行仍然成立。
- 凡改动 **库仲裁规则（`Merge`）或落点推导（`LibraryLocation`）**：更新 §1 对应行 + §5.7/§5.8。
- 凡改动 **SyncPackages** 的 diff/失效/rule 逻辑：重点更新 §4 症状表 + §5 对应条目（这是 bug 高发区）。
- 凡改动 **`CanReuseLaunchPlan` 的条件**：更新 §2 关键不变量 + §4「每次部署都重建启动计划」行。
- 新增 **rule 选择器类型**：现在直接读全量 `Package`（`locked.Resolved`），无重建/无假值——只需确认该字段在 `Package` 上（§5.4）。
- 凡新增 **格式转换的丢失项**：产出 `LaunchPlanDiagnostic`（无法表达用 Error、有偏差用 Warning），不要静默丢弃；诊断出口固定在 `ImporterAgent`/`ExporterAgent`。
- 凡改动 **来源覆盖仲裁 / SourceOrders / FlattenPackages / SuppressedBy**：见 `notes/DeploymentPriority.md`（POLY-117 专属 note）。
