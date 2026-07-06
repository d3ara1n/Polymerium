# LockData 现代化：实施影响面与故障定位地图

> 制定日期：2026-07-06
> 定位：POLY-116（LockData 现代化）实施后的**影响面与故障定位地图**。不是施工蓝本——设计源自 [POLY-116](https://d3ara1n.atlassian.net/browse/POLY-116)（施工蓝本已随实施归档）；本文档面向"上线后遇到问题，如何快速定位责任域和出错点"。随代码演进维护：凡改动 lock 相关行为，同步更新本文档。
> 关联：[POLY-116](https://d3ara1n.atlassian.net/browse/POLY-116)
> 当前状态：实施完成（2026-07-06）

---

## 0. 先理解重构本质（90% 的问题靠这三点定位）

POLY-116 把 `data.lock.json` 从"构建过程缓存"重做为"版本锁定权威源"。与旧架构的三个根本差异，理解了就能定位绝大多数异常：

1. **双对象模型** —— 每次部署同时存在 `BaseLock`（磁盘旧 lock，只读参照）和 `Lock`（本次产物，new 出来逐步填）。**各 stage 判断有效性看 BaseLock、修改写 Lock**，两者解耦。任何"判断依据"问题先想：它是看 BaseLock（稳定源头）还是 Lock（被改的中间态）？
2. **固定线性 pipeline** —— `DeployEngine.DecideNext` 状态机已废弃，8 个 stage 固定顺序线性执行，各自看 BaseLock 自决干不干活。没有"跳过 stage"的分支了——出问题不要找状态机，找具体 stage 的自决逻辑。
3. **防漂移 = lock 优先** —— 只要 lock 里有 resolved 就不重新 resolve（不碰仓库 API）；rule 变化用缓存 resolved 重算，绝不重新 resolve。任何"版本不对"先想：它该走缓存复用、还是 resolve，有没有走错。

---

## 1. 责任域地图（按模块）

| 责任域 | 文件 | 职责 | 出错查这里 |
|--------|------|------|-----------|
| **数据结构** | `TridentCore.Abstractions/FileModels/LockData.cs` | lock 的内存与磁盘形态。`FORMAT=2`，含 `PlatformData/ViabilityData/ArtifactData/LockedPackage/ResolvedPackage/PackageRule/Compatibility/FileHashes` | 序列化/反序列化异常、字段缺失、跨机器数据不一致 |
| **FastMode 门控** | `TridentCore.Abstractions/Extensions/LockDataExtensions.cs` | `Verify(setup, optionsHash)`——FastMode 快速启动的判定，**完整 purl（含 vid）集合比较** | "改了 mod 版本没生效（FastMode）"、"每次都全量部署" |
| **部署上下文** | `TridentCore.Core/Engines/Deploying/DeployContext.cs` | `BaseLock`/`Lock`/`OptionsHash` 的载体 | stage 间数据传递问题 |
| **pipeline 编排** | `TridentCore.Core/Engines/DeployEngine.cs` | 固定 8-stage 线性序列 | stage 执行顺序、stage 未执行（看 Sequence 数组） |
| **加载** | `Stages/LoadLockStage.cs` | 读磁盘→BaseLock；new Lock 填 Platform/Viability；旧 `FORMAT=1` 降级 BaseLock=null | lock 读不到、升级后全量重建、Platform 填错 |
| **平台缓存·vanilla** | `Stages/InstallVanillaStage.cs` | 看 `BaseLock.Platform==Lock.Platform`→整体迁 artifact；否则重建 vanilla（调 PrismLauncher + authlib-injector） | vanilla 启动缺库、artifact 迁移不完整 |
| **平台缓存·loader** | `Stages/ProcessLoaderStage.cs` | 看 `BaseLock.Platform` 匹配→skip（artifact 已整体迁）；否则重建 loader | loader 启动失败、loader 库缺失、mainClass 错 |
| **版本锁定·核心** | `Stages/SyncPackagesStage.cs` | diff（按 project identity）+ floating 失效（platform/optionsHash）+ 固定 vid 变更检测 + 精细 rule（零网络）| **绝大多数 mod 版本相关问题**（见 §4） |
| **持久化** | `Stages/PersistLockStage.cs` | 唯一写回磁盘点 | lock 没更新、写文件失败 |
| **下载清单** | `Stages/GenerateManifestStage.cs` | 消费 `Lock.Artifact`+`Lock.Packages`；package 的 target 由 `Rule`+`Resolved` 派生（`ComputeRelativeTarget`） | mod 落盘路径错、文件没进 manifest、target 不相对 |
| **下载执行** | `Stages/SolidifyManifestStage.cs` | 下载 + 软链；**下载后 hash 校验闭环** | 下载失败、hash 不匹配、缓存未命中重复下载 |
| **规则重算** | `Engines/Deploying/PackagePlanner.cs` | `RecomputeRule`（缓存 resolved 重建 Input，零网络）/ `EvaluateRule` / `ResolveAsync`；`PlanAsync` 保留供导出器/宿主 | rule 评估结果错、rule 改了没生效 |
| **库累积** | `TridentCore.Core/Extensions/LockDataExtensions.cs` | `MakeIgniter` + `AddLibrary`/`AddLibraryPrismFlavor`（库去重规则） | 库重复、库丢失、启动 classpath 错 |
| **启动装配** | `TridentCore.Core/Services/InstanceManager.cs` | FastMode 路径（读 lock→Verify→直接 Launch）；正常路径 foreach 驱动 stage | FastMode 误判、stage 进度跟踪、启动参数装配 |
| **PrismLauncher 适配** | `TridentCore.Core/Services/PrismLauncherService.cs` | `AddValidatedLibrariesToArtifact(IList<Library>, ...)` | vanilla/loader 库获取 |
| **账户注入** | `Services/AccountConfigurerAgent.cs` + `Accounts/AuthlibAccountConfigurer.cs` | 读 `Lock.Artifact` 装配启动 | 账户/外置登录启动问题 |

---

## 2. 部署 pipeline 各 stage（运行时顺序，按症状定位）

固定序列，**每个都执行**（无跳过），各自内部自决：

```
LoadLock        读 BaseLock + 建 Lock(Platform/Viability)
   ↓
InstallVanilla  BaseLock.Platform 匹配且 Artifact 非空 → 整体迁；否则重建 vanilla
   ↓
ProcessLoader   BaseLock.Platform 匹配 → skip；否则重建 loader
   ↓
SyncPackages    按 purl diff → floating/platform 失效重 resolve + 固定 vid 变更重 resolve
                + 精细 rule（缓存重算）→ 组装 Lock.Packages
   ↓
PersistLock     写 data.lock.json（唯一写回）
   ↓
EnsureRuntime   查 Java 运行时
   ↓
GenerateManifest  从 Lock 生成下载清单（package target 派生）
   ↓
SolidifyManifest  下载 + 软链 + 下载后 hash 校验
```

**关键不变量**：`InstallVanilla` 与 `ProcessLoader` 的判断**都看 `BaseLock.Platform`**（不看 Lock 中间态）。匹配时 InstallVanilla 整体迁、ProcessLoader skip；不匹配时两者各自重建。如果出现"vanilla 迁了但 loader 没迁"或反之，查这两个 stage 的判断是否一致。

---

## 3. 数据流（追踪 LockData 去向）

```
磁盘 data.lock.json
   │ 读（LoadLockStage；FORMAT≠2 或损坏 → BaseLock=null）
   ▼
BaseLock (只读参照) ──┐
                      ├─► 各 stage 判断有效性
Lock (new, 逐步填) ◄──┘    各 stage 迁移/重建写入
   │
   │ 写（PersistLockStage，唯一）
   ▼
磁盘 data.lock.json

旁路（不进 pipeline）：
磁盘 ──► InstanceManager FastMode ──► Verify ──► 命中直接 Launch / 不命中进 pipeline
```

`LockedPackage` 的 `Purl` 字段存**当前声明的完整 purl**（含 vid），是 diff 键也是 FastMode Verify 的比较单元。`Resolved` 是锁定的事实。`Rule` 是 rule 评估快照。

---

## 4. 症状 → 定位速查表（核心 debug 价值）

| 症状 | 首查 | 次查 / 说明 |
|------|------|------------|
| **mod 版本漂移到最新** | `SyncPackagesStage` floating 失效判定 | 该走缓存复用却 resolve 了——查 `platformChanged` 是否误判 true，或 purl 的 MatchKey 是否没匹配上 BaseLock |
| **改了固定 mod 版本（@vid）启动还是旧版** | `LockDataExtensions.Verify` | FastMode 用完整 purl 比较；若仍命中旧版，查 Verify 的 purl 集合是否含 vid |
| **rule 改了，mod 行为/路径没变** | `SyncPackagesStage` 精细 rule + `PackagePlanner.RecomputeRule` | rule 应零网络重算；查 `ReconstructPackage` 重建的 Input 字段是否够 rule 选择器用（Label/Kind/Ns/Pid） |
| **每次启动都重新解析（不缓存）** | `Verify`（FastMode）+ `LoadLockStage`（BaseLock 读取） | FastMode 不命中→查 platform/viability.optionsHash/purl 集合哪个变了；或 BaseLock 读失败（FORMAT/损坏） |
| **启动崩溃 / class not found / 主类错** | `InstallVanilla`/`ProcessLoader` artifact 迁移 + `MakeIgniter` | artifact 迁移不完整、loader 重建遗漏、Library 去重把需要的库 dedup 掉 |
| **mod 文件落盘路径不对** | `GenerateManifestStage.ComputeRelativeTarget` | target 由 Rule+Resolved 派生；查 Normalizing/Destination 逻辑 |
| **跨机器复制实例后版本不一致** | `LockData` 可迁移字段 | 应无本地 Key；查 ResolvedPackage 是否含足够复现信息（vid/url/hashes） |
| **下载的文件损坏未被发现** | `SolidifyManifestStage` hash 校验 | 下载后应有 `FileHelper.VerifyModified` 闭环；查 `ResolvedPackage.Hashes.Primary` |
| **旧实例升级后第一次全量重建** | `LoadLockStage` FORMAT 降级 | `FORMAT=1` 反序列化失败→BaseLock=null→全量，属预期（数据无损） |
| **loader 相关启动失败（Fabric/Forge/Quilt/NeoForge）** | `ProcessLoaderStage` 重建分支 | 仅 platform 不匹配时才重建；查 loader 字符串解析、intermediary（Fabric/Quilt）、ForgeWrapper 参数 |
| **authlib-injector/外置登录失败** | `AccountConfigurerAgent` + `AuthlibAccountConfigurer` | 读 `Lock.Artifact`；查 artifact 是否就绪 |
| **加 mod 不解析 / 删 mod 不移除** | `SyncPackagesStage` diff 三桶 | Added/Removed/Matched 分桶；查 MatchKey（project identity 小写）匹配 |
| **同 project 多版本冲突** | 待 POLY-117（部署优先级） | 本任务只保证锁定后不漂移，冲突仲裁未实现 |

---

## 5. 已知复杂点 / 易藏 bug

1. **`SyncPackagesStage.MatchKey`** —— 用 `(Label, Namespace, Pid)` 小写化做 diff 键。大小写、namespace 缺失（`null`→`""`）处理若变，会导致匹配失败→误判新增/删除→重新 resolve。filter/vid 故意不进 key（支持 fixed→floating 继承）。
2. **floating 失效只看 `platformChanged`** —— 因为 `ResolveAsync` 的 filter 完全从 `setup.Version/Loader` 构建，不从 `entry.Purl` 的 `#filter` 取。**若未来 filter 语义改变**（让 entry 的 filter 参与 resolve），此处必须同步加 entry filter 变更检测，否则 filter 漂移。
3. **固定 vid 变更检测** —— `parsed.Vid != locked.Resolved.Vid` 触发重 resolve。这是对蓝本的补全（尊重用户重定版本）。若用户反馈"锁定太死，改 vid 不生效"，先查这里。
4. **`PackagePlanner.ReconstructPackage`** —— 精细 rule 用缓存 resolved 重建 `RuleHelper.Input`，只填 rule 选择器读的字段，其余是占位（`Reference="sourced://recompute"`、`Author=""` 等）。**若新增 rule 选择器类型**（依赖新字段），ReconstructPackage 必须补该字段，否则该选择器永远 false。
5. **artifact 整体迁移的原子性** —— InstallVanilla 匹配则整体迁、ProcessLoader 匹配则 skip。两者判断**必须都看 `BaseLock.Platform`**。若任一改成看 Lock 中间态，会出现"半迁移"。
6. **FastMode Verify 完整 purl** —— 含 vid 比较，所以改 vid 会触发重建。若改成 project identity 会重蹈"改 vid 不生效"覆辙。
7. **`FORMAT=2` 破坏性** —— 旧 `FORMAT=1` 反序列化失败→降级。若以后再改结构，升 FORMAT 并保证旧版能 clean 降级（lock 可重建，不做双读迁移）。
8. **`PackagePlanner.PlanAsync` 保留** —— 导出器（4 个）+ 宿主（`InstancePackageModal`/`InstanceSetupPageModel`）依赖。它内部委托 `ResolveAsync`+`EvaluateRule`，与 pipeline 共享底层，无重复逻辑。若动 ResolveAsync/EvaluateRule 签名，PlanAsync 也要同步。

---

## 6. 降级行为与外部依赖

| 场景 | 行为 | 出错点 |
|------|------|--------|
| `data.lock.json` 不存在 | BaseLock=null，全量构建 | 正常首次部署 |
| `FORMAT=1`（旧版）文件 | 反序列化失败→BaseLock=null→全量重建（数据无损） | `LoadLockStage` catch JsonException |
| 文件损坏 | 同上 | `LoadLockStage` catch |
| `Options.FullCheckMode=true` | 不读 BaseLock→全量重建 | `LoadLockStage` 顶部条件 |
| PrismLauncher Meta 不可达 | InstallVanilla/ProcessLoader 重建失败→部署中断 | 网络依赖，无降级 |
| 仓库 API（CF/Modrinth）失败 | 仅 SyncPackages 的 toResolve 集合受影响；已锁定的包零网络不受波及 | resolve 失败提示用户 |
| authlib-injector API 失败 | InstallVanilla 重建失败 | 部署中断 |

---

## 7. 改动文件全清单（按责任域，实施完成态）

**新增**
- `TridentCore.Core/Engines/Deploying/Stages/LoadLockStage.cs`
- `TridentCore.Core/Engines/Deploying/Stages/SyncPackagesStage.cs`
- `TridentCore.Core/Engines/Deploying/Stages/PersistLockStage.cs`
- `TridentCore.Abstractions/Exceptions/LockUnavailableException.cs`

**删除**
- `Stages/CheckArtifactStage.cs` / `BuildArtifactStage.cs` / `ResolvePackageStage.cs`
- `Engines/Deploying/LockDataBuilder.cs` / `LockDataBuilderExtensions.cs` / `LibraryCollectionExtensions.cs`（后者内容合入 `Core/Extensions/LockDataExtensions.cs`）
- `Exceptions/ArtifactUnavailableException.cs`

**重写/重大修改**
- `Abstractions/FileModels/LockData.cs`（结构重构）
- `Abstractions/Extensions/LockDataExtensions.cs`（Verify 重写）
- `Core/Engines/Deploying/DeployContext.cs` / `DeployEngine.cs`
- `Core/Engines/Deploying/Stages/InstallVanillaStage.cs` / `ProcessLoaderStage.cs` / `GenerateManifestStage.cs` / `SolidifyManifestStage.cs` / `EnsureRuntimeStage.cs`
- `Core/Engines/Deploying/PackagePlanner.cs`（拆 RecomputeRule/EvaluateRule/ResolveAsync）
- `Core/Extensions/LockDataExtensions.cs`（合入 AddLibrary）
- `Core/Services/InstanceManager.cs` / `PrismLauncherService.cs` / `AccountConfigurerAgent.cs`
- `Accounts/AuthlibAccountConfigurer.cs`
- `DeployStage.cs`（枚举改名）
- `Resources.resx` / `Resources.zh-hans.resx` / `Resources.Designer.cs`（资源键）

**宿主（src/Polymerium.Avalonia）**
- `PageModels/InstanceHomePageModel.cs`（stage→resource 映射）
- `Services/Instances/DeployTracker.cs`（默认 stage）
- 经 grep 确认无其他 Artifact/Watermark/Parcels 旧名残留

---

## 8. 维护约定

- 凡改动 lock 的**结构**（新增/改字段）：升 `LockData.FORMAT`，更新本文件 §1 数据结构行 + §5 相关易错点。
- 凡改动 **pipeline stage** 顺序或职责：更新 §2 + §1 对应行。
- 凡改动 **SyncPackages** 的 diff/失效/rule 逻辑：重点更新 §4 症状表 + §5 对应条目（这是 bug 高发区）。
- 凡改动 **Verify**：更新 §4 的 FastMode 相关行。
- 新增 **rule 选择器类型**：检查 `ReconstructPackage` 是否需补字段（§5.4）。
