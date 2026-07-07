# 部署优先级（按分组）：实施影响面与故障定位地图

> 制定日期：2026-07-07
> 定位：POLY-117（按分组部署优先级）实施后的**影响面与故障定位地图**。面向"上线后遇到多来源 mod 覆盖 / 冲突 / SuppressedBy 相关问题，如何快速定位"。施工蓝本见 `plans/DEPLOYMENT-PRIORITY-BY-GROUP.md`（已标 ✅ 已实施）。随代码演进维护：凡改动来源覆盖仲裁或 lock 包结构，同步更新本文档。
> 关联：[POLY-117](https://d3ara1n.atlassian.net/browse/POLY-117)；上游 [POLY-116](LockData.md)、[POLY-118](SOURCE-REFERENCE-SEMANTICS)；下游 POLY-119（分组 UI，读锁的 `SuppressedBy`）、POLY-120（Recipe，写 `recipe://` Source）。
> 当前状态：实施完成（2026-07-07）

---

## 0. 核心变化（定位问题的四个锚点）

POLY-117 让"同实例存在来源不同但目标冲突的包"成为常态并确定性解决。与旧架构的差异：

1. **身份键 `project` → `(project, Source)`** —— `SyncPackagesStage` 不再按 project identity 把同 mod 多来源压扁成一条；同 project 不同 source 各自独立锁定、各自复用，全部存活到 `FlattenPackages`。任何"同 mod 多来源没裁/裁错"先想：身份键是否含 Source。
2. **两遍仲裁（FlattenPackages，新增 stage）** —— 插在 SyncPackages 与 PersistLock 之间。先用 project identity 仲裁（同 mod 多来源），再在幸存者里用落盘路径仲裁（不同 mod 撞文件）。两遍同一 `Arbitrate` 例程。胜者 `SuppressedBy=null`、败者 `SuppressedBy=胜者.Purl`；同档平手 throw。
3. **SourceOrders 覆盖模型** —— 包的覆盖力 `(Tier, Index)`：手动(3) > 列表内 SourceOrders(2，末个最高) > 未列非整合包(1) > 当前整合包 Setup.Source(0)。**仲裁单位是 source，不是 project 名或文件名**。
4. **PriorityHash（FastMode 专用指纹）** —— `SourceOrders`/`Setup.Source` 变了让 FastMode 的 `Verify` 失效，否则会原样启动旧胜者。**只影响 FastMode**；正常全量部署每次都重跑 FlattenPackages，天然正确。
5. **全量 Package 存储** —— `LockedPackage.Resolved` 直接存完整 `Package`（非 slim 投影）。规则复算、清单、未来 UI 都读真实数据；删了 `ResolvedPackage`/`Compatibility`/`FileHashes`/`ReconstructPackage`/`RecomputeRule`。

---

## 1. 责任域地图（按模块）

| 责任域 | 文件 | 职责 | 出错查这里 |
|--------|------|------|-----------|
| **覆盖模型** | `Abstractions/FileModels/Profile.cs`（`Rice`） | `SourceOrders: IList<string>`——来源覆盖顺序，末个=最高 | "顺序没生效"、"哪个 source 该赢" |
| **覆盖力计算** | `Core/Engines/Deploying/Stages/FlattenPackagesStage.cs:RankOf` | `(Tier,Index)` 档位判定 | 胜负判反 → 查 SourceOrders.IndexOf / Setup.Source 比较 |
| **身份键** | `Core/Engines/Deploying/Stages/SyncPackagesStage.cs` | `Key=(Label,Namespace,Pid,Source)`；diff/迁移/复用 | 同 mod 多来源被压扁/丢失 → 查 Key 是否含 Source |
| **两遍仲裁** | `Stages/FlattenPackagesStage.cs:Arbitrate` | project 遍（全量）+ path 遍（幸存者）；同档平手 throw | "没裁"/"裁错"/"PackageConflictException" |
| **落盘路径** | `Core/Utilities/PackagePathHelper.cs` + `Core/Extensions/LockDataExtensions.cs`（`RelativeTarget()` 扩展） | normalize 后的 folder+filename，path 遍的仲裁 key + 清单 target | 路径错/撞车判错 → 查 Normalizing/Destination |
| **FastMode 门** | `Abstractions/Extensions/LockDataExtensions.cs:Verify` | 加 `PriorityHash` 比较（`SourceOrders`+`Setup.Source`） | "改了 SourceOrders 启动还是旧胜者" → Verify 没失效 |
| **指纹生成** | `Core/Utilities/ViabilityHashHelper.cs` | `PriorityOf(setup)` + `OptionsOf(options)`；PriorityOf = hash(Source + Join(SourceOrders)) | PriorityHash 不稳 → 查展平顺序 |
| **锁包结构** | `Abstractions/FileModels/LockData.cs` | `LockedPackage(Purl, Source, Package Resolved, PackageRule Rule, string? SuppressedBy)` | 序列化/字段问题；**Resolved 是全量 Package** |
| **异常** | `Core/Exceptions/PackageConflictException.cs` | 同档平手 throw → deploy Faulted | "Unresolvable package conflict" 报错 |
| **清单跳过** | `Stages/GenerateManifestStage.cs` | `SuppressedBy != null` 的包不进 manifest（不下载/不软链） | "锁里有但没落盘" → 查 SuppressedBy |
| **pipeline** | `Core/Engines/DeployEngine.cs:Sequence` | 9 stage，FlattenPackages 插在 SyncPackages 后 | stage 没跑 → 查 Sequence |
| **stage 枚举/名** | `DeployStage.cs`（`SyncPackages`/`FlattenPackages`）+ resx | ResolvePackage 已对齐为 SyncPackages | UI stage 名错 → 查 GetStageTitle 映射 |
| **入口** | `Core/Services/InstanceManager.cs` | 传 `ViabilityHashHelper.OptionsOf`/`PriorityOf` 给 Verify；FlattenPackages case 只推进 StageStream | FastMode 误判 / priorityHash 没传 |

> **仲裁是内部事务**：不向外部发运行时报告（无 ConflictStream、无 PackageConflict 类型）。冲突的"谁压了谁"留在锁的 `SuppressedBy` 里，UI（POLY-119）读锁还原。唯一外漏信号是 `PackageConflictException`（同档平手）。

---

## 2. FlattenPackages 在 pipeline 中的位置

```
LoadLock → InstallVanilla → ProcessLoader → SyncPackages → FlattenPackages → PersistLock → EnsureRuntime → GenerateManifest → SolidifyManifest
                                                      ↑ 新增 ↑
```

- **SyncPackages** 产出 `Lock.Packages`：每个 `(project, source)` 一条，matched 包 `SuppressedBy=null`（只有 FlattenPackages 写 SuppressedBy）。
- **FlattenPackages** 纯变换 `Lock.Packages`，两遍：
  1. **project 遍**（全量，key=`label|ns|pid`）—— 同 project 多来源按 `(Tier,Index)` 裁决。这是**核心场景，与 Normalizing 无关**（不靠文件名撞路径）。
  2. **path 遍**（仅 project 幸存者，key=`RelativeTarget()`）—— 不同 project 撞同一落盘路径，同样裁决。
  - 合并：project 抑制者 + path 结果（胜者+败者）。计数守恒。
- **PersistLock** 落盘（败者也进锁，`SuppressedBy` 指向胜者）。
- **GenerateManifest** 跳过 `SuppressedBy != null` 的包。

**关键不变量**：SuppressedBy 只在 FlattenPackages 写、在 SyncPackages matched 重建时重置为 null（败者被删/胜者变时能自动升迁）。

---

## 3. 数据流（SourceOrders → SuppressedBy → 清单）

```
Profile.Rice.SourceOrders ──┐
Setup.Source ───────────────┤
                             ├─► FlattenPackages.RankOf → (Tier,Index)
LockedPackage.Source ────────┘         │
                                       ▼
                            project 遍 / path 遍 dedupe
                                       │
                          胜者 SuppressedBy=null ◄──┐
                          败者 SuppressedBy=胜者.Purl ┤ 持久化进 lock
                                       │              │
                                       ▼              ├─► Verify(FastMode)：PriorityHash 变 → 失效
                            GenerateManifest 跳过 SuppressedBy ┘
```

`ViabilityHashHelper.PriorityOf(setup)` = `hash(Setup.Source + "\n"-Join(SourceOrders))`。显式展平保证**内容+顺序**都进指纹。

---

## 4. 症状 → 定位速查表（核心 debug 价值）

| 症状 | 首查 | 次查 / 说明 |
|------|------|------------|
| **改了 SourceOrders，启动还是旧胜者** | `Verify` 的 `PriorityHash` | FastMode 没失效——查 PriorityHash 是否进了 Verify、ViabilityHashHelper.PriorityOf 展平是否漏了顺序 |
| **同 mod 多来源（整合包+手动/recipe）没裁，两个都落盘** | `FlattenPackages` project 遍 + `SyncPackages` 身份键 | 身份键是否含 Source（旧 project-only 会先压扁）；RankOf 档位是否判反 |
| **`PackageConflictException`：Unresolvable package conflict** | `FlattenPackages.Arbitrate` 同档平手 | 两个包 source 同档（如都手动、或两个未入列 recipe）→ 引擎不偷偷拍板，要用户排 SourceOrders 或删重复 |
| **锁里有某 mod、但 build/ 里没有** | `SuppressedBy` | 该包被裁了——读 SuppressedBy 找胜者；属预期（被覆盖），不是 bug |
| **删了胜者后，下次启动败者没自动顶上** | `SyncPackages` matched 重建 + `FlattenPackages` | matched 重建重置 SuppressedBy=null；败者若成组内唯一即升胜者、复用锁定版本不重解析 |
| **覆盖顺序（手动/recipe/整合包）判反** | `RankOf` 档位表 | 手动(Source=null)=3 > 列表内=2(末最高) > 未列非整合包=1 > 整合包(==Setup.Source)=0 |
| **不同 mod 撞同一文件名没处理** | `FlattenPackages` path 遍 | path 遍只在 project 幸存者里跑；若没跑查 Sequence/幸存者过滤 |
| **PriorityHash 变了却触发了 floating 重解析** | `SyncPackages.platformChanged` | 重解析只看 platform，不看 PriorityHash——若误触发，查 platformChanged 是否被污染 |
| **旧 dev 锁升级后第一次全量重建** | `LoadLockStage` | 全量 Package 结构与旧 ResolvedPackage 不兼容 → 反序列化失败 → BaseLock=null → 重建（预期、无害） |
| **rule 改了 mod 行为/路径没变** | `SyncPackages`（matched 走 `EvaluateRule` 离线复算） | 全量 Package 直接喂 RuleHelper，无假值；查 rule 选择器读的字段是否在 Package 里 |
| **导出器/独立规划路径路径算错** | `PackagePathHelper.RelativeTarget` | 三处（Flatten/Manifest/ToPlan）共用同一公式；查 Normalizing/Destination |

---

## 5. 已知复杂点 / 易藏 bug

1. **身份键含 Source** —— `Key=(Label,Namespace,Pid,Source)`。filter/vid 故意不进 key（支持 fixed→floating 继承）。**若改键粒度**，多来源存活/压扁行为直接变。
2. **两遍顺序 project → path** —— project 先解决覆盖语义（核心），path 在幸存者里收尾跨 project 撞车。顺序不影响同 project 结果（project 遍总能抓到），但影响 SuppressedBy 语义清晰度。
3. **project 胜者被 path 挤空时无回填** —— 若 project A 的胜者 A1 在 path 遍输给 project B（`SuppressedBy=B1`），A 的亚军 A2 不会自动顶上（A2 仍是 project 遍败者，`SuppressedBy=A1`）。结果 project A 整个不落盘。**这是有意的**：自动提升亚军要么违反用户声明的 source 优先级、要么需要不动点算法；现状靠 SuppressedBy 可见（POLY-119 能展示），交给用户调整。罕见，别当成 bug 去打补丁。
4. **同档平手直接 throw** —— 引擎不偷偷字符串 tiebreak。两手动同路径、或两个未入列 recipe 撞、或同组重复 → `PackageConflictException` 阻断。这是 setup 错误的正确导向。
5. **SuppressedBy 进锁、跨部署留存** —— 败者不落盘但版本留锁，避免覆盖顺序调整时重解析。**若改"锁只记最终状态"原则**，会破坏"改 SourceOrders 后败者秒升胜者、零网络"。
6. **PriorityHash 只进 Verify，不进 platformChanged** —— 故意分离：SourceOrders 变只让 FastMode 失效重跑 pipeline，不触发 floating 重解析（重解析由 platformChanged 门控，仅 platform）。
7. **全量 Package 持久化** —— `LockedPackage.Resolved` 是完整 `Package`（含 Thumbnail/Author/Summary/Reference/Dependencies 等）。Reference 是 Uri 友情链接（≠ Source 覆盖身份，二者绝不可混）。旧 ResolvedPackage 结构的锁读到会反序列化失败 → 全量重建。
8. **RelativeTarget 不挂 LockedPackage** —— `FileHelper` 在 Core，挂 Abstractions 的 LockedPackage 会反向依赖。改用 Core 扩展方法 `locked.RelativeTarget()`。**若把 RelativeTarget 挪进 Abstractions**，需先把 Sanitize/GetAssetFolderName 下沉。
9. **仲裁无运行时报告** —— 删过 ConflictStream/PackageConflict。**别重新加回"实时冲突流"**——锁的 SuppressedBy 是冲突事实的唯一权威源，UI 读它即可（POLY-119）。

---

## 6. 降级行为

| 场景 | 行为 | 出错点 |
|------|------|--------|
| SourceOrders 为空 | 退化为档位默认：手动>未列非整合包>整合包 | RankOf 的 tier 0/1/3 分支 |
| 同档平手（不可决议） | `PackageConflictException` → deploy Faulted → 用户看到撞的包与来源 | FlattenPackages throw |
| 旧 dev 锁（ResolvedPackage 结构） | 反序列化失败 → BaseLock=null → 全量重建一次 | LoadLockStage catch |
| project 胜者被 path 挤空 | 该 project 不落盘，SuppressedBy 记录可见 | 预期、非 bug（§5.3） |
| SourceOrders 改动 + FastMode | PriorityHash 变 → Verify 失效 → 重跑 pipeline 重仲裁 | 正常 |
| SourceOrders 改动 + 非 FastMode | FlattenPackages 每次都重跑，天然正确 | 无需指纹 |

---

## 7. 改动文件全清单（实施完成态）

**Trident（submodules/Trident.Net）**
- `Abstractions/FileModels/LockData.cs` —— `LockedPackage.Resolved` 改全量 `Package` + `SuppressedBy`；`ViabilityData`+`PriorityHash`；删 `ResolvedPackage`/`Compatibility`/`FileHashes`（FORMAT 不变）
- `Abstractions/FileModels/Profile.cs` —— `Rice.SourceOrders`
- `Abstractions/Extensions/LockDataExtensions.cs` —— `Verify` 增 `PriorityHash`
- `Core/Utilities/ViabilityHashHelper.cs`（新，含 PriorityOf + OptionsOf）
- `Core/Utilities/PackagePathHelper.cs`（新）—— RelativeTarget 唯一公式
- `Core/Extensions/LockDataExtensions.cs` —— `RelativeTarget()` 扩展
- `Core/Exceptions/PackageConflictException.cs`（新）
- `Core/Engines/Deploying/Stages/FlattenPackagesStage.cs`（新）
- `Core/Engines/Deploying/Stages/SyncPackagesStage.cs` —— 身份键+Source；matched 重建 SuppressedBy=null
- `Core/Engines/Deploying/PackagePlanner.cs` —— ResolveAsync ToLookup+Distinct；删 ReconstructPackage/RecomputeRule
- `Core/Engines/Deploying/Stages/GenerateManifestStage.cs` —— 跳过 SuppressedBy；删 ComputeRelativeTarget；字段随 Package 改名
- `Core/Engines/DeployEngine.cs` / `DeployContext.cs` —— Sequence 加 FlattenPackages；priorityHash 透传
- `Core/Engines/Deploying/DeployStage.cs` —— `FlattenPackages`；`ResolvePackage`→`SyncPackages`
- `Core/Engines/Deploying/Stages/LoadLockStage.cs` —— Viability 带 PriorityHash
- `Core/Services/InstanceManager.cs` —— 传 PriorityOf；FlattenPackages case
- `Core/Services/Instances/DeployTracker.cs` —— 净零改动（ConflictStream 加了又删）

**Polymerium（src/Polymerium.Avalonia）**
- `PageModels/InstanceHomePageModel.cs` + `Properties/Resources.resx`/`.zh-hans.resx`/`Designer.cs` —— `FlattenPackages` 展示名；`SyncPackages` 键/值（概念从"解析"转"同步"）

**已删**
- `PackageConflict.cs`（冲突报告类型，仲裁改为内部事务）

---

## 8. 维护约定

- 凡改动 **SourceOrders 语义或 RankOf 档位**：更新 §0.3 + §1 覆盖力计算行 + §4 胜负相关行。
- 凡改动 **FlattenPackages 仲裁算法**（遍数/key/顺序/平手处理）：更新 §2 + §5 对应条目（bug 高发区）。
- 凡改动 **SyncPackages 身份键**：更新 §0.1 + §5.1。
- 凡改动 **Verify / PriorityHash**：更新 §0.4 + §4 FastMode 行 + §5.6。
- 凡改动 **LockedPackage 结构**（Resolved/SuppressedBy）：同步更新 `notes/LockData.md`（锁结构权威 note）+ 本文 §1 锁包结构行。
- **别重新引入** ConflictStream/PackageConflict 运行时报告（§5.9）—— 除非有明确的实时消费需求，且即便如此也优先让 UI 读锁。
