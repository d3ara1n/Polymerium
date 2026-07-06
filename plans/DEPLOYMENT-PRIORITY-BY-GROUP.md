# 按分组部署优先级

> 制定日期：2026-06-29
> 定位：部署引擎任务，Recipe 系统的前置条件。允许同实例存在来源不同但目标路径冲突的包，按来源组覆盖顺序确定性解决冲突。
> 当前状态：蓝本
> Jira：[POLY-117](https://d3ara1n.atlassian.net/browse/POLY-117)
> 依赖：[POLY-116](https://d3ara1n.atlassian.net/browse/POLY-116)（✅ LockData 现代化、版本锁定）、[POLY-118](SOURCE-REFERENCE-SEMANTICS.md)（✅ Source 归属分类）。下游：[POLY-119](PACKAGE-GROUPING-UI.md)（分组 UI，消费 `SourceOrders` + 冲突报告）、[POLY-120](RECIPE-SYSTEM.md)（Recipe 系统，写 `recipe://` Source）。

> 术语：实例级与包级来源字段都叫 **Source**（`Profile.Rice.Source` / `Profile.Rice.Entry.Source`）。本文"当前整合包来源"指 `Setup.Source`。

---

## 0. 本任务在 Recipe 系统中的位置

> Recipe 系统总纲：[RECIPE-SYSTEM.md](RECIPE-SYSTEM.md)

Recipe 让一个实例同时挂整合包 + recipe + 手动包成为常态，这些来源可能带来同一个 mod。当前部署引擎对这种重复没有任何覆盖顺序机制——同路径先到先得、静默丢弃后者，谁赢取决于不可控的列表顺序。本任务建立确定性的、按来源组的覆盖顺序解决。组顺序（`SourceOrders`）与 PACKAGE-GROUPING-UI 共用：UI 的组排序就是部署的覆盖顺序。

---

## 1. 背景与动机

### 1.1 现状：部署引擎无覆盖顺序概念

部署 pipeline 固定 8 阶段（`DeployEngine.cs:38-48` 的 `Sequence`）：

```
LoadLock → InstallVanilla → ProcessLoader → SyncPackages → PersistLock → EnsureRuntime → GenerateManifest → SolidifyManifest
```

`SyncPackagesStage`（`Stages/SyncPackagesStage.cs`）把所有启用包解析成 `LockedPackage` 写进 `Lock.Packages`，**不去重**。`GenerateManifestStage:46-48` 遍历 `Lock.Packages` 产 `FragileFile`，每个包一条，**不按 target 分组**：

```csharp
foreach (var locked in Context.Lock.Packages)
{
    if (locked.Rule.Skipping) continue;
    ...
    manifest.FragileFiles.Add(new(sourcePath, targetPath, ...));   // 重复 target 也照加
}
```

`SolidifyManifestStage.UpsertProjection`（`Stages/SolidifyManifestStage.cs:205-228`）做投影去重，但所有包都赋 `ProjectionPriority.Package = 0`：

```csharp
private enum ProjectionPriority { Package = 0, Live = 1, Persist = 2 }

private void UpsertProjection(... targetPath, ProjectionPriority priority, ...)
{
    if (projections.TryGetValue(targetPath, out var existing))
    {
        if (priority > existing.Priority)   // 包对包: 0 > 0 = false → 永远跳过
            projections[targetPath] = new(...);
        else
            logger.LogDebug("... keeps ... over ...");   // 静默丢弃
        return;
    }
    projections[targetPath] = new(...);
}
```

`ProjectionPriority` 的三值区分**文件类型**（包 vs live vs persist），不是包之间的优先级。所有包共享 `Package = 0`。

### 1.2 问题：重复包的不确定行为

当两个包算出相同 `RelativeTarget`（`GenerateManifestStage.cs:112-121` 的 `ComputeRelativeTarget`：路径是 `mods/{FileName}` 或 `{Destination}/{FileName}`，同一 mod 同版本必然同名）时：

1. `Lock.Packages` 里两个 `LockedPackage` 并存（锁记录了重复）。
2. `GenerateManifest` 产两条同 target 的 `FragileFile`。
3. `SolidifyManifest.UpsertProjection` 包对包分支 `0 > 0 = false` → **先入字典者胜**。"先入"追溯到 `Lock.Packages` 顺序，再追溯到 `SyncPackages` 的 `result` 列表拼接顺序（matched ∩ base + added），对用户完全不可见、不可控。

### 1.3 动机

GitHub #73（"把两个小型整合包合并到一起……生怕误删了自己的补充模组"）与 recipe 的诉求一致：来源叠加是常态，部署结果必须可预测、可解释。recipe 和手动包冲突时，用户手动加的本应胜出；整合包为保证兼容用的旧版本，遇到基底 recipe 的盲目最新时本应保留——这些都要求确定性的、按来源组的覆盖顺序。

---

## 2. 目标 / 非目标 / 不做的事

**目标**

1. **允许重复**：同一 mod 可以来自不同 Source 共存于 Profile，部署引擎不当错误。
2. **确定性覆盖顺序**：同 target 冲突时按来源组覆盖顺序解决，上层覆盖下层。
3. **冲突可见**：冲突在 UI 警告（哪个包被谁覆盖），部署不报错中断。
4. **锁定一致**：胜方版本进锁；败方版本也留锁（避免覆盖顺序调整时重新解析），并标记 `SuppressedBy` 指向胜者。
5. **覆盖顺序可重排**：`SourceOrders` 改变后，FastMode 失效以重算冲突，胜负按新顺序翻转。
6. **覆盖顺序 = 组顺序**：与 PACKAGE-GROUPING-UI 的组排序同源（共用 `SourceOrders`）。

**非目标**

- 不实现 recipe 本身（POLY-120）。
- 不改包的部署路径算法（`ComputeRelativeTarget` 的 `mods/{FileName}` / `{Destination}` 规则不变）。
- 不在部署失败时自动选替代版本——冲突解决是"选已存在的谁落盘"，不是"找替代"。
- 不做分组 UI 渲染（POLY-119）；本任务只交付分组依据（`SourceOrders`）+ 冲突数据通道。

**不做的事**

- **不允许多整合包** —— `Setup.Source` 单值，整合包带副作用不可叠加。
- **不因冲突中断部署** —— 冲突是预期状态，仅 UI 警告。
- **不把分组持久化成 Profile 嵌套结构** —— 分组是 `Source` 派生视图。
- **不让 Trident 认识 "recipe"** —— 覆盖顺序只用 `Setup.Source` + `SourceOrders` 两份 profile 数据，recipe/modpack/legacy 的"种类"分类留在 Polymerium 的 `PackageSourceHelper` 服务 UI（§3.4）。
- **不升 FORMAT** —— recipe 全链路（POLY-115–120）未发版，无存量锁需保护，结构变更就地落（§3.6）。
- **不修 `FastMode` 入 `OptionsHash` 的 smell** —— per-call 提示混进持久化指纹会在快/慢调用交替时振荡、白跑全量；属单独收口项，当前低效非 bug。

---

## 3. 核心设计

### 3.1 冲突的定义

**冲突 = 两个或以上包计算出相同的 `RelativeTarget`**（即部署后会落到 `build/` 下同一文件路径）。与现有 `UpsertProjection` 的 `targetPath` 字典键完全对齐。

> 不以"同 project identity"判冲突——同一 mod 不同版本（JEI 1.20.1 vs 1.20.4）文件名不同、路径不同，各落各的，不算冲突。只有"同路径"才算。

### 3.2 覆盖顺序模型（`SourceOrders`）

`Profile.Rice` 新增字段：

```csharp
// Profile.Rice（FileModels/Profile.cs）
public IList<string> SourceOrders { get; init; } = [];   // source URI，覆盖顺序，末个 = 最高
```

`SourceOrders` 是**覆盖顺序（使用顺序）**，不是"优先级"：靠前者先铺、靠后者覆盖前者（追加即覆盖）。把它读作图层栈，靠后 = 更上层 = 覆盖力更强。

单个包的覆盖力档位（高 → 低）：

| 档位 | 判定 | 备注 |
|------|------|------|
| 最高 | `Source == null` | 手动，显式添加，恒在栈顶 |
| 列表档 | `SourceOrders.Contains(Source)` | 在列表里的按列入顺序，**末个最高**（后者覆盖前者） |
| 中档 | 未列出 且 `Source != Setup.Source` | recipe / legacy / 其他来源 |
| 最低 | 未列出 且 `Source == Setup.Source` | 当前整合包（未列入时为基底） |

列表档整体高于中档——进入 `SourceOrders` 即声明"我是显式覆盖层"。同级 tiebreak：`Source` 字符串升序，再 `Purl` 字符串升序，保证完全确定。Trident 全程不需要 `InternalUriHelper.IsKind(,"recipe")`。

**典型场景**：

- 整合包 + 手动同 mod：手动(顶) > 整合包(底) → 手动胜
- recipe + 手动：手动 > recipe(中档) → 手动胜
- 整合包 + recipe（无手动，`SourceOrders` 空）：recipe(中档) > 整合包(底) → recipe 胜
- 两 recipe（都在 `SourceOrders`）：列表后者胜（覆盖前者）
- **基底 recipe 用例**：用户把整合包 `Source` 列入 `SourceOrders`、recipe 不列 → 整合包(列表档) > recipe(中档) → 整合包旧版本胜，覆盖 recipe 的盲目最新

### 3.3 冲突解决：`SyncPackages` 的尾 pass

不新增 stage。`SyncPackagesStage` 在 `result` 拼装完成、写回 `Context.Lock` **之前**追加一道孤立的去重 pass：

```
输入: List<LockedPackage> result（已解析、已算 rule）, Setup.Source, Setup.SourceOrders
1. 对每个包算 RelativeTarget（提取自 GenerateManifestStage.ComputeRelativeTarget 的共享 helper）
2. 按 RelativeTarget 分组 → 每组 ≥2 即冲突
3. 组内按 §3.2 覆盖力排序，最高者为胜者，其余为败者
4. 败者: locked with { SuppressedBy = winner.Purl }
5. 产出: result（败者仍在列表，带 SuppressedBy）；同时产出冲突报告 PackageConflict[]
```

败者**保留在 `Lock.Packages`**（版本不丢、覆盖顺序调整时无需重解析），仅被打上 `SuppressedBy`。这道 pass 是纯变换：只读 `Lock.Packages` 候选 + `Context.Setup`，只写各 `LockedPackage.SuppressedBy`，不碰 Platform/Artifact/BaseLock、不调仓库、不重新解析。

> 合进 `SyncPackages` 而非独立 stage：胜者由各包自身 `Source` + `SourceOrders` 派生，没有外部仲裁者——这是"去重 + tiebreak"不是"仲裁"，SyncPackages 此刻手里就有 dedup 所需的一切。合并让 pipeline 保持 8 阶段，免掉 `DeployStage` 枚举 / `InstanceManager` stage-switch / `InstanceHomePageModel` 映射三处改动。约束：dedup 必须是 `result` 拼装后的孤立尾 pass，不得掺进 diff/resolve/rule 核心（notes/LockData.md §5 标的 bug 高发区）。详见备选 E.1。

**每次部署的成本分布**：resolve 是网络 I/O，已按版本锁定最优跳过——只有 added/失效项打网。`RecomputeRule` 与 dedup 尾 pass 则每次必跑，但都是廉价离线 CPU（字符串/枚举计算，百级包毫秒内），全量重算保证始终新鲜、无缓存一致性问题。不为它们设跳过：检测"是否变了"的 fingerprint 比对成本 ≈ 重算本身；而 FastMode 命中时整条 pipeline 已被跳过，所以 `SyncPackages` 真正运行时必有实质变化——这两步不是瓶颈。

### 3.4 `SuppressedBy`：锁里的诊断字段

```csharp
// LockData.LockedPackage（FileModels/LockData.cs:42-47）
public record LockedPackage(
    string Purl,
    string? Source,
    ResolvedPackage Resolved,
    PackageRule Rule,
    Compatibility? Compat,
    string? SuppressedBy = null    // null=生效；非 null=被该 purl 的包覆盖
);
```

`SuppressedBy` 存**胜者的 Purl**。胜者的完整 `LockedPackage` 也在锁里，UI/诊断按 purl 反查即得"被谁、什么版本、哪个来源"覆盖。

`SuppressedBy` 是锁里为"谁覆盖了谁"开的诊断字段——部署结果的核心事实，不是额外元数据。败者留在 `Lock.Packages`、不产 `FragileFile`、不落盘；胜者被删时败者升为胜者，复用锁定版本无需重解析。

**完整性自愈**：下一轮部署 `SyncPackages` 重建包时显式置 `SuppressedBy = null`，dedup pass 全量重算。胜者被删则败者升为胜者、`SuppressedBy` 归 null，无悬空引用。

### 3.5 冲突报告与 UI 通道

新增记录类型（`TridentCore.Abstractions`）：

```csharp
public record PackageConflict(
    string TargetPath,
    PackageConflictSide Winner,
    IReadOnlyList<PackageConflictSide> Losers);

public record PackageConflictSide(string Purl, string? Source, string DisplayName);
```

`SyncPackagesStage` 暴露 `Subject<IReadOnlyList<PackageConflict>> ConflictStream`（仿 `SolidifyManifestStage.ProgressStream`）。`DeployTracker` 加同名 `ConflictStream`，`InstanceManager.DeployCoreAsync` 的 foreach switch 里订阅（与 Solidify 的 ProgressStream 订阅同模式）。冲突报告**不落盘**——它是部署结果副作用，FastMode 命中时无新鲜报告；UI 的冲突警告渲染（读锁的 `SuppressedBy` + 按需反查胜者）归 POLY-119，可脱离运行时报告独立工作。

### 3.6 Viability 指纹职责分离

`Verify`（FastMode 重部署门，`LockDataExtensions.cs:14-30`）回答"这把锁还能不能信"；`SyncPackages.platformChanged`（floating 重解析门）回答"解析 filter 的输入变没变"。两件事的输入不同，各自一份指纹：

| 指纹 | 服务于 | 含 |
|------|--------|-----|
| `OptionsHash`（既有） | `Verify`（重部署门） | FastMode/ResolveDependency/FullCheckMode 开关 |
| `PriorityHash`（新） | `Verify`（重部署门） | `Setup.Source` + `Setup.SourceOrders` |
| `platformChanged` | `SyncPackages`（floating 重解析门） | 仅 platform（Version/Loader）+ `BaseLock==null` |

```csharp
// LockData.ViabilityData（FileModels/LockData.cs:31）
public record ViabilityData(string OptionsHash, string? PriorityHash = null);
```

- `PriorityHash` 只在 `Verify` 里比较：覆盖顺序变了 → FastMode 失效 → 进 pipeline → `SyncPackages` 的 dedup 按新 `SourceOrders` 重算。
- `PriorityHash` **不进 `platformChanged`**：floating 解析的 filter 只依赖 platform，与优先级无关；优先级变更只触发廉价的 dedup 重算，不触发 floating 重解析。

`PriorityHash` 由一个共享 helper 算（如 `ViabilityHash.PriorityOf(setup)`），`InstanceManager` 写入锁、`Verify` 比对，**同一份计算**避免漂移。`platformChanged` 已只看 platform（见 `SyncPackagesStage.cs` 的 NOTE 注释），无需本任务再动。

> **FORMAT 保持 2**：recipe 全链路（POLY-115–120）未发版，没有存量 FORMAT=2 锁要保护。本任务给 `ViabilityData` 加 `PriorityHash`、给 `LockedPackage` 加 `SuppressedBy`，都就地落、不升 FORMAT——不发版就不存在 break change。dev 期间旧构建的锁被新构建读到，顶多 `JsonException → BaseLock=null → 全量重建`一次，无害。下一次 FORMAT 变更是 recipe 发版后若有结构改动时的事。

### 3.7 Profile 数据"已分组"的含义

部署引擎读 `Entry.Source` + `Setup.Source` + `Setup.SourceOrders` 现场判定覆盖力，Profile 维持扁平 `IList<Entry>`。分组是 `Source` 维度的派生视图，不是物理结构。

---

## 4. 改动面

### Trident（submodules/Trident.Net）

| 层 | 文件 | 改动 |
|----|------|------|
| 锁结构 | `Abstractions/FileModels/LockData.cs:31` | `ViabilityData` 加 `string? PriorityHash = null`（FORMAT 不变，仍 2） |
| 锁结构 | `Abstractions/FileModels/LockData.cs:42-47` | `LockedPackage` 末尾加 `string? SuppressedBy = null` |
| FastMode 门 | `Abstractions/Extensions/LockDataExtensions.cs:14-30` | `Verify` 增 `PriorityHash` 比较（从入参 `setup` 现算） |
| 新增类型 | `Abstractions/`（新文件） | `PackageConflict` / `PackageConflictSide` record |
| 新增 helper | `Abstractions/` 或 `Core/`（新文件） | `ViabilityHash.PriorityOf(setup)`——`InstanceManager` 写入与 `Verify` 比对共用，避免漂移 |
| Profile | `Abstractions/FileModels/Profile.cs` (`Rice`) | 加 `IList<string> SourceOrders = []` |
| 路径 helper | 提取自 `GenerateManifestStage.cs:112-121` | `ComputeRelativeTarget(rule, resolved)` 提到共享位置（`PackagePlanner` 静态方法或 `PackagePathHelper`），供 `SyncPackages` dedup 与 `GenerateManifest` 共用 |
| **冲突解决** | `Core/Engines/Deploying/Stages/SyncPackagesStage.cs` | ① `result` 拼装后、写回 `Context.Lock` 前加 dedup 尾 pass（算 target → 分组 → 选胜者 → 败者 `SuppressedBy = winner.Purl`，产出 `PackageConflict[]` 推 `ConflictStream`）；② matched 重建 `locked with { … }`（~L71）显式带 `SuppressedBy = null`；③ `BuildLocked`（~L97）构造时 `SuppressedBy = null`；④ 加 `Subject<IReadOnlyList<PackageConflict>> ConflictStream` |
| 清单 | `Core/Engines/Deploying/Stages/GenerateManifestStage.cs:46-48` | `if (locked.Rule.Skipping) continue;` → `if (locked.Rule.Skipping \|\| locked.SuppressedBy is not null) continue;` |
| 投影 | `Core/Engines/Deploying/Stages/SolidifyManifestStage.cs:205-228` | 无需改动。包对包分支变为事实防御代码（败者已在 GenerateManifest 被跳过，不会两条同 target） |
| 上下文 | `Core/Engines/Deploying/DeployContext.cs:6,28` | 加 `string priorityHash` 构造参数 + `PriorityHash` 属性（对称 `OptionsHash`） |
| 引擎 | `Core/Engines/DeployEngine.cs:5,40` | 透传 `priorityHash` 参数 |
| 加载 | `Core/Engines/Deploying/Stages/LoadLockStage.cs:48-52` | 建 `Lock` 时 `Viability = new(OptionsHash, PriorityHash)` |
| 入口 | `Core/Services/InstanceManager.cs:240-250` | 算 `priorityHash = ViabilityHash.PriorityOf(profile.Setup)` 传入 engine；foreach switch 里订阅 `SyncPackagesStage.ConflictStream` → `tracker.ConflictStream`（仿 Solidify 的 ProgressStream 订阅） |
| 跟踪器 | `Core/Services/Instances/DeployTracker.cs` | 加 `Subject<IReadOnlyList<PackageConflict>> ConflictStream`（+ `Dispose`） |

> `DeployEngine.Sequence`（`DeployEngine.cs:38-48`）与 `DeployStage` 枚举不改——这是合并进 `SyncPackages` 的直接收益。

### Polymerium（src/Polymerium.Avalonia）

| 层 | 文件 | 改动 |
|----|------|------|
| 状态聚合 | `Services/InstanceStateAggregator.cs`（或等价处） | 订阅 `DeployTracker.ConflictStream`，把冲突纳入实例状态快照 |
| UI 渲染 | —— | **本任务不做**。冲突警告标识、败者"被 X 覆盖"展示归 POLY-119（读锁 `SuppressedBy` + 按 purl 反查胜者即可，不依赖运行时报告） |

> `Utilities/PackageSourceHelper.cs`（POLY-118 产物）不改。覆盖顺序已完全由 Trident 用 `Setup.Source`+`SourceOrders` 计算；`PackageSourceHelper.Classify` 继续只服务 UI 分组标签。

---

## 5. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 整合包 + 手动同 mod（同路径） | 手动版落盘，整合包版 `SuppressedBy=手动purl`；UI 可警告 |
| 2 | recipe + 手动同 mod | 手动版胜，recipe 版 `SuppressedBy` 指向手动 |
| 3 | 整合包 + recipe 同 mod（无手动，`SourceOrders` 空） | recipe 版胜（中档 > 底） |
| 4 | 两个 recipe 同 mod（都在 `SourceOrders`） | 列表后者胜（覆盖前者） |
| 5 | 无冲突 | 行为与现状一致，零警告，`SuppressedBy` 全 null |
| 6 | 冲突部署 | 不报错，正常启动，仅 UI 警告 |
| 7 | 改 `SourceOrders` 或 `Setup.Source` 后启动 | FastMode 失效（`PriorityHash` 变）→ 进 pipeline → 胜负翻转；floating 包不重解析 |
| 8 | 基底 recipe 用例 | 整合包 `Source` 入 `SourceOrders`、recipe 不入 → 整合包旧版本胜，recipe 版 `SuppressedBy` 指向整合包 |
| 9 | 冲突败方的锁定版本 | 留在 `Lock.Packages`（`SuppressedBy` 非 null），不落盘；胜者被删后下次部署败者升为胜者、复用锁定版本无需重解析 |
| 10 | 不同 mod 同名 jar（路径撞） | 按覆盖顺序解决，列为冲突，同级按 purl 字符序 tiebreak（确定） |
| 11 | FORMAT | 保持 2，不升；旧 dev 构建的锁读到顶多全量重建一次 |

---

## 6. 风险与取舍

| 风险 | 取舍 |
|------|------|
| `SyncPackages` 已是 bug 高发区，再加 dedup | dedup 是 `result` 拼装后的孤立尾 pass，不碰 diff/resolve/rule 核心；若未来冲突逻辑需跨 stage 信息或长复杂，再抽独立 stage |
| `SuppressedBy` 破坏"锁只记最终状态"惯例 | 这是 scoped 诊断字段——正当因为"谁覆盖谁"是部署结果核心事实；不扩成塞诊断的先例 |
| FastMode 与覆盖顺序变更的耦合 | 独立 `PriorityHash`（只进 `Verify` 不进 `platformChanged`），职责分离，避免误触发 floating 重解析 |
| Trident 不知 recipe 种类，默认档依赖"当前整合包 = `Setup.Source`" | 此判定只用 profile 数据，无需 recipe 知识；分层干净，recipe 分类留 Polymerium |
| 同级 tiebreak 依赖 purl 字符序（对人无意义） | 仅求确定性，避免回到"先到先得"；真实同级冲突罕见 |

---

## 7. 不做的事（明确边界）

- **不支持多整合包** —— `Setup.Source` 单值。
- **不改部署路径规则** —— `ComputeRelativeTarget` 的 `mods/{FileName}` / `{Destination}` 不变。
- **不自动找替代版本** —— 冲突解决是"选已存在的谁落盘"。
- **不因冲突中断部署**。
- **不把分组持久化成 Profile 嵌套**。
- **不让 Trident 认识 recipe** —— 覆盖顺序只用 `Setup.Source`+`SourceOrders`。
- **不做冲突 UI 渲染** —— 归 POLY-119。
- **不扩 `OptionsHash`** —— 覆盖顺序变更走独立 `PriorityHash`（§3.6）。
- **不升 FORMAT** —— recipe 未发版，结构变更就地落（§3.6）。
- **不修 `FastMode` 入 `OptionsHash` 的 smell** —— 单独收口项。

---

## 附录：备选方案备案

### E.1 冲突解决的位置

当前选：**合并进 `SyncPackages` 的尾 pass**（§3.3）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 独立 `ResolvePackageConflictsStage`（插 SyncPackages 与 PersistLock 间） | 8→9 阶段 | 胜者无外部仲裁者（纯 `Source`+`SourceOrders` 派生），是"去重"非"仲裁"；独立 stage 多牵动 `DeployStage` 枚举/stage-switch/`InstanceHomePageModel` 映射三处，收益不抵成本。留作"dedup 长复杂时"的逃生通道 |
| 折进 GenerateManifest | 那里本就现算 target | 在 PersistLock 之后，败者的 `SuppressedBy` 进不了锁；且 GenerateManifest 职责变杂 |
| 折进 SolidifyManifest 的 UpsertProjection | 改 `Package` 优先级为来源感知 | 在 PersistLock 之后同上；且会下载败者（浪费带宽），报告混入文件操作 stage |

### E.2 败方的处理

当前选：**留 `Lock.Packages` + `SuppressedBy` 指向胜者**（§3.4）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 败方不进锁 | 仅胜者进 | 覆盖顺序调整时败者被当 Added → 重新解析（网络），违"锁定不漂移" |
| `bool Suppressed`（不带胜者） | 二值 | 锁不自描述"被谁覆盖"，UI/FastMode 需另查；选 `SuppressedBy` 开诊断口子 |
| 败方落盘到 `mods/.disabled/` | 备用目录 | 改变 MC 加载语义，引入复杂度；启停已有 `Enabled`，不混用 |
| 败方从 Profile 删 | 冲突即删 | 破坏性，失失败者信息，覆盖顺序调整无法恢复 |

### E.3 FastMode 与覆盖顺序变更的耦合

当前选：**独立 `PriorityHash`（只进 `Verify`）**（§3.6）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 扩大 `OptionsHash` 纳入 Source/SourceOrders | 一份指纹 | `SyncPackages` 用 `OptionsHash` 判 `platformChanged`，扩进去让覆盖顺序变更误触发 floating 重解析（语义错；未来有 floating 包会白白打网络） |
| 不做（接受 FastMode 命中时冲突不重算） | —— | 改 `SourceOrders` 后 build/ 仍是旧胜者软链，用户调整不生效，违目标 5 |

### E.4 SourceOrders 的范围

当前选：**通用来源排序（整合包也可入列）**（§3.2）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| `RecipeOrder`（仅 recipe） | 整合包恒底 | 无法表达基底 recipe 用例（用户要整合包旧版本压过 recipe 最新版）；能力被限死 |
| 固定三档不可重排 | Manual>Recipe>Modpack | 同上，缺灵活性 |

### E.5 SourceOrders 方向

当前选：**末个 = 最高（追加即覆盖）**——`SourceOrders` 是覆盖顺序（使用顺序），靠前者先铺、靠后者覆盖

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 首个 = 最高（"拖到顶 = 胜"） | 按 UI"置顶=最重要"惯例 | 与"覆盖顺序"语义相左：用户心智是"先整合包打底、再 recipe 叠加、手动最后覆盖"，列表应读作应用序列而非优先级排名；UI（POLY-119）以图层栈呈现更贴合 |
