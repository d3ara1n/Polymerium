# 按分组部署优先级

> 制定日期：2026-06-29
> 定位：部署引擎任务，Recipe 系统的前置条件。允许同实例存在来源不同但目标路径冲突的包，按来源组覆盖顺序确定性解决冲突。
> 当前状态：✅ 已实施（2026-07-07）
> Jira：[POLY-117](https://d3ara1n.atlassian.net/browse/POLY-117)
> 依赖：[POLY-116](https://d3ara1n.atlassian.net/browse/POLY-116)（✅ LockData 现代化）、[POLY-118](SOURCE-REFERENCE-SEMANTICS.md)（✅ Source 归属分类）。下游：[POLY-119](PACKAGE-GROUPING-UI.md)（分组 UI，消费 `SourceOrders` + 冲突报告）、[POLY-120](RECIPE-SYSTEM.md)（Recipe 系统，写 `recipe://` Source）。

> 术语：实例级与包级来源字段都叫 **Source**（`Profile.Rice.Source` / `Profile.Rice.Entry.Source`）。本文"当前整合包来源"指 `Setup.Source`。

> 实施偏离（2026-07-07，均因 layering / 一致性，详见 §3.6、§3.7、§4）：
> - `RelativeTarget` 不作 `LockedPackage` 计算属性（`FileHelper` 在 Core，挂 Abstractions 会反向依赖）→ Core 层 `PackagePathHelper` + `LockedPackage.RelativeTarget()` 扩展方法，三处共用。
> - `PackageConflictException` 落 `Core/Exceptions`（非 `Abstractions/Exceptions`）——与其余异常同归一处。
> - 仲裁改为**两遍**（project identity + 落盘路径），同一 `Arbitrate` 例程。原计划的纯 path 分组只在 `Normalizing=true` 时让同 project 撞路径，默认 `Normalizing=false` 下核心用例失效——见 §3.1。
> - `LockedPackage.Resolved` 直接存全量 `Package`（非 slim `ResolvedPackage` 投影）；连带删 `ResolvedPackage`/`Compatibility`/`FileHashes` 三个嵌套记录、删 `PackagePlanner.ReconstructPackage`/`RecomputeRule`——规则复算直接喂存储的 Package，假值 footgun 消失；锁变富，解锁被卡的 UI 计划（§3.6）。
> - 冲突裁决是**内部事务**，产出稳定结果即可；外部只通过 `PackageConflictException`（同档平手）感知"裁不了"。删 `ConflictStream`（无消费者的死管道）、删 `PackageConflict` 报告类型（UI 读锁的 `SuppressedBy` 还原冲突，归 POLY-119）。

---

## 0. 本任务在 Recipe 系统中的位置

> Recipe 系统总纲：[RECIPE-SYSTEM.md](RECIPE-SYSTEM.md)

Recipe 让一个实例同时挂整合包 + recipe + 手动包成为常态，这些来源可能带来同一个 mod。当前部署引擎不允许同 project 多来源共存（在 `SyncPackages` 的 diff 阶段被按 project identity 压扁，见 §1）。本任务把身份单位细化到 `(project, Source)`，让多来源各自锁定存活，再由独立的 `FlattenPackages` stage 按覆盖顺序解决同路径冲突。

---

## 1. 背景与动机

### 1.1 现状：同 project 多来源在 diff 阶段就被压扁

部署 pipeline 固定 8 阶段（`DeployEngine.cs:38-48`）：

```
LoadLock → InstallVanilla → ProcessLoader → SyncPackages → PersistLock → EnsureRuntime → GenerateManifest → SolidifyManifest
```

`SyncPackagesStage` 用 `Dictionary<Key, Entry>` 按 **project identity**（`MatchKey = Label/Namespace/Pid`，故意不含 vid 以支持 fixed→floating 继承锁定版本）建 diff 视图：

```csharp
// SyncPackagesStage.cs:27-32
var setupByKey = new Dictionary<Key, Profile.Rice.Entry>();
foreach (var entry in enabled)
    setupByKey[MatchKey(entry.Purl)] = entry;   // 同 project 后者 overwrite 前者
```

这暗含世界观契约"每个 project 只有一个包"。所以整合包 JEI + 手动 JEI 这种同 project 不同来源的两条 Entry，进 diff 前就被字典压成一条（列表里靠后的胜、另一条静默丢弃）。

`PackagePlanner.ResolveAsync` 是第二处：`index.ToDictionary(x => x.Key, ...)`（`PackagePlanner.cs:56`），`Key = (Label, Namespace, Pid, Vid)` 含 vid——同 project 同版本不同来源会撞键、抛 `ArgumentException`。当前不抛，正是因为 `setupByKey` 先压扁了。

### 1.2 落到 `SolidifyManifest` 的只有"不同 mod 同名 jar"

因为同 project 在 `SyncPackages` 已压扁，`GenerateManifest` 产出的 `FragileFile` 里每个 project 只一条。`SolidifyManifest.UpsertProjection`（`SolidifyManifestStage.cs:205-228`）的包对包分支（所有包 `ProjectionPriority.Package = 0`，`0 > 0 = false` → 先入字典者胜）只在"不同 project 同 filename"时触发——即验收场景 10。

### 1.3 动机

GitHub #73 与 recipe 的诉求：来源叠加是常态（整合包 + recipe + 手动可能带同一个 mod），部署结果必须可预测、可解释。整合包为保证兼容用的旧版本遇到基底 recipe 的盲目最新时应保留——这些要求"同 project 多来源共存 + 按覆盖顺序确定性解决"。

---

## 2. 目标 / 非目标 / 不做的事

**目标**

1. **允许同 project 多来源共存**：身份单位从 `project` 细化到 `(project, Source)`，各自独立锁定/复用。
2. **确定性覆盖顺序**：同 target 冲突按来源组覆盖顺序解决，上层覆盖下层。
3. **冲突可见**：跨档（可决议）冲突在 UI 警告，部署不阻断；同档（不可决议）冲突直接报错、阻断、告诉用户怎么修。
4. **锁定一致**：胜方版本进锁；败方版本也留锁（避免覆盖顺序调整时重解析），标 `SuppressedBy` 指向胜者。
5. **覆盖顺序可重排**：`SourceOrders` 改变后 FastMode 失效以重算冲突，胜负翻转。
6. **覆盖顺序 = 组顺序**：与 PACKAGE-GROUPING-UI 共用 `SourceOrders`。

**非目标**

- 不实现 recipe 本身（POLY-120）。
- 不改部署路径算法（`mods/{FileName}` / `{Destination}` 不变）。
- 不在部署失败时自动选替代版本。
- 不做分组 UI 渲染（POLY-119）；只交付 `SourceOrders` + 冲突数据通道。

**不做的事**

- 不允许多整合包（`Setup.Source` 单值）。
- 不把分组持久化成 Profile 嵌套（分组是 `Source` 派生视图）。
- 不让 Trident 认识 "recipe"（覆盖顺序只用 `Setup.Source` + `SourceOrders`）。
- 不升 FORMAT（recipe 全链路未发版，结构变更就地落；见 §3.8）。
- 不修 `FastMode` 入 `OptionsHash` 的 smell（per-call 提示混进持久化指纹，单独收口）。
- 不对同档冲突搞隐藏 tiebreak——直接报错（§3.5）。

---

## 3. 核心设计

### 3.1 冲突的定义

仲裁算法统一为：**找出 key 出现多次的组，组内按 source 覆盖力裁决，胜者落盘、败者 `SuppressedBy` 指向胜者；source 无法决议（同档平手）则报错阻断。** key 跑两遍（§3.5）：

- **project identity**（`label/ns/pid`）——同 project 多来源（整合包 + recipe + 手动可能带同一个 mod）。这是来源覆盖的核心场景，**与 `Normalizing` 无关**（不依赖文件名是否归一）。
- **落盘路径**（`RelativeTarget`，即 normalize 后的 folder+filename）——不同 project 撞到 `build/` 下同一文件。

> 不靠"同路径"判同 mod 冲突——那只在 `Normalizing=true` 时偶然成立（版本无关文件名撞），默认 `Normalizing=false` 下整合包旧版本与 recipe 新版本文件名不同、各落各的，核心用例反而失效。所以同 project 仲裁以 project identity 为 key，与文件名脱钩。

### 3.2 覆盖顺序模型（`SourceOrders`）

```csharp
// Profile.Rice（FileModels/Profile.cs）
public IList<string> SourceOrders { get; init; } = [];   // source URI，覆盖顺序，末个 = 最高
```

`SourceOrders` 是**覆盖顺序（使用顺序）**：靠前者先铺、靠后者覆盖前者（追加即覆盖）。读作图层栈，靠后 = 更上层 = 覆盖力更强。

包的覆盖力档位 → 比较键 `(int Tier, int Index)`，大者胜：

| 档位 | 判定 | (Tier, Index) |
|------|------|---------------|
| 最高 | `Source == null`（手动） | (3, 0) |
| 列表档 | 在 `SourceOrders` 中 | (2, indexOf)，末个最高 |
| 中档 | 未列出 且 `Source != Setup.Source` | (1, 0) |
| 最低 | 未列出 且 `Source == Setup.Source`（当前整合包） | (0, 0) |

列表档整体高于中档（进 `SourceOrders` 即声明"我是显式覆盖层"）。Trident 全程不需要 `InternalUriHelper.IsKind(,"recipe")`。

**典型场景**：整合包+手动同 mod → 手动(3)胜；recipe+手动 → 手动胜；整合包+recipe（`SourceOrders` 空）→ recipe(1) > 整合包(0)；两 recipe 都在 `SourceOrders` → 列表后者胜；**基底 recipe 用例**：整合包 `Source` 入列、recipe 不入 → 整合包(2) > recipe(1)，整合包旧版本胜。

### 3.3 `SyncPackages` 身份键 `project` → `(project, Source)`（主体工作）

改 `Key` 加 `Source`，让同 project 不同来源各自存活到 `FlattenPackages`：

```csharp
// SyncPackagesStage.cs —— 原 record Key(string Label, string Namespace, string Pid)
private record Key(string Label, string Namespace, string Pid, string? Source);

// 原 MatchKey(string purl) 拆成接收 Source
private static Key MatchKey(string purl, string? source)
{
    PackageHelper.TryParse(purl, out var parsed);
    return new(
        (parsed.Label ?? string.Empty).ToLowerInvariant(),
        parsed.Namespace ?? string.Empty,
        parsed.Pid ?? string.Empty,
        source);
}
```

调用点：
```csharp
foreach (var entry in enabled)
    setupByKey[MatchKey(entry.Purl, entry.Source)] = entry;
// baseByKey 同理用 locked.Purl, locked.Source
```

`matched/added/removed` 三桶逻辑（`Intersect`/`Except`）不变，只是键更细。matched 重建**显式重置 `SuppressedBy = null`**（只有 `FlattenPackages` 写 `SuppressedBy = 胜者`，`SyncPackages` 不持有仲裁状态）：

```csharp
result.Add(locked with { Purl = entry.Purl, Source = entry.Source, Rule = rule, SuppressedBy = null });
```

> fixed→floating 继承语义不破坏：`(project, source)` 不因 vid 变而变，同 source 内版本漂移仍匹配、仍继承锁。

### 3.4 `ResolveAsync`：`ToDictionary` → `ToLookup`（第二处）

同 project 同版本不同来源现在会进 `ResolveAsync`，`Key`（含 vid）相同。改成"resolve 一次、结果分发给所有同 Key 的 Entry"：

```csharp
// PackagePlanner.cs:ResolveAsync
var resolved = await agent
    .ResolveBatchAsync(index.Select(x => x.Key).Distinct(), filter)   // 去重，避免重复请求
    .ConfigureAwait(false);

var byKey = index.ToLookup(x => x.Key, x => x.Origin);   // 原 ToDictionary → ToLookup
return resolved
    .SelectMany(x => byKey[x.Item1].Select(origin => (origin, x.Item2)))
    .ToList();
```

### 3.5 `FlattenPackages` stage（新增，独立解决冲突）

插在 `SyncPackages` 与 `PersistLock` 之间。纯变换 `Lock.Packages`，用同一 `Arbitrate` 例程跑两遍（§3.1）：先按 project identity 仲裁（全量），再在 project 幸存者里按 `RelativeTarget` 仲裁。每遍：key 唯一则直通；key 重复则按 `(Tier, Index)` 排序、胜者 `SuppressedBy = null`、败者 `SuppressedBy = 胜者.Purl`；同档平手抛 `PackageConflictException` 阻断。

```csharp
// FlattenPackagesStage.OnProcessAsync —— 两遍同例程，key 与 subject 不同
var afterProject = Arbitrate(Context.Lock.Packages, ProjectKeyOf,
    (_, w) => w.Resolved.ProjectName, setup);              // pass 1: project identity（全量）
var afterPath = Arbitrate(
    afterProject.Where(p => p.SuppressedBy is null),       // 仅 project 幸存者进 pass 2
    p => p.RelativeTarget(),                               // pass 2: 落盘路径
    (target, _) => target, setup);

// Arbitrate: GroupBy(key) → 单员直通；多员 RankOf 排序 → 唯一顶胜(直通)、其余 SuppressedBy=胜者.Purl；
//            同档 >1 顶 → throw PackageConflictException(subject, collisions)
// RankOf:       (Tier,Index) 手动 3 > 列表 2(末最高) > 未列非整合包 1 > 整合包 0
// ProjectKeyOf: PackageHelper.TryParse(purl) → label.ToLowerInvariant()|ns|pid
// 合并: afterProject 里已被 project 抑制的 + afterPath（path 胜者 + path 败者）
```

> 不做字符串 tiebreak。同档平手是用户 setup 问题（同组重复、或多个未入列的 recipe/modpack 撞路径），引擎不偷偷拍板，直接抛错指明撞的包和来源。

### 3.6 `LockedPackage` 形状 + `RelativeTarget`

```csharp
// LockData.cs —— FORMAT 不变（仍 2）
public record LockedPackage(
    string Purl,
    string? Source,
    Package Resolved,            // 全量 Package 原样存（非 slim 投影）
    PackageRule Rule,
    string? SuppressedBy = null);   // null=生效；非 null=被该 purl 的包覆盖
```

`Resolved` 直接存全量 `Package`：规则复算（`EvaluateRule`）、清单生成、未来 UI 都读真实数据，无需 `ReconstructPackage` 重建、无假值 footgun。连带删除 `ResolvedPackage`/`Compatibility`/`FileHashes` 三个嵌套记录（`Requirements` 在 Package 里、`Compat` 从未被读）、删 `PackagePlanner.ReconstructPackage`/`RecomputeRule`（matched 包规则复算直接 `EvaluateRule(entry, locked.Resolved, rules)`）。

> `RelativeTarget` 不作 `LockedPackage` 计算属性——`FileHelper`（`Sanitize`/`GetAssetFolderName`）在 Core 层，挂到 Abstractions 的 `LockedPackage` 上会制造 Abstractions→Core 的反向依赖。改为：
> - `Core/Utilities/PackagePathHelper.RelativeTarget(...)` 持有唯一公式；
> - `Core/Extensions/LockDataExtensions.cs` 暴露 `locked.RelativeTarget()` 扩展方法；
> - `FlattenPackages`、`GenerateManifest`、`PackagePlanner.ToPlan` 三处共用，顺手收敛了 ToPlan 里第三处历史重复。

`GenerateManifestStage` 删 `ComputeRelativeTarget`、改用 `locked.RelativeTarget()`；跳过抑制包；字段随 Package 改名（`Vid→VersionId`、`Url→Download`、`Hashes.Primary→Hash`）：

```csharp
// GenerateManifestStage.cs
foreach (var locked in Context.Lock.Packages)
{
    if (locked.Rule.Skipping || locked.SuppressedBy is not null) continue;
    ...
    var targetPath = Path.Combine(PathDef.Default.DirectoryOfBuild(Context.Key), locked.RelativeTarget());
    manifest.FragileFiles.Add(new(sourcePath, targetPath, locked.Resolved.Download, locked.Resolved.Hash));
}
```

### 3.7 仲裁是内部事务（无运行时报告）

冲突裁决完全在 `FlattenPackagesStage` 内部完成，产出稳定的 `Lock.Packages`（胜者 `SuppressedBy = null`、败者 `SuppressedBy = 胜者.Purl`），不向外部发布运行时报告。**唯一外漏的信号是 `PackageConflictException`**——同档平手无法决议时 throw → deploy `Faulted` → 用户看到错误。

冲突的"谁压了谁"事实留在锁里（败者的 `SuppressedBy` 指向胜者），UI 渲染归 POLY-119 读锁还原，不需要运行时事件流。故不设 `ConflictStream`、不设 `PackageConflict` 报告类型。

### 3.8 Viability 指纹职责分离（`PriorityHash` + FORMAT 保持 2）

```csharp
// LockData.cs —— ViabilityData 加字段
public record ViabilityData(string OptionsHash, string? PriorityHash = null);

// TridentCore.Core/Utilities/ViabilityHashHelper.cs（新文件；InstanceManager 算好两个指纹传给 Verify）
public static class ViabilityHashHelper
{
    public static string PriorityOf(Profile.Rice setup) =>
        HashHelper.ComputeObjectHash(new { setup.Source, Order = string.Join('\n', setup.SourceOrders) });

    public static string OptionsOf(DeployOptions options) => HashHelper.ComputeObjectHash(options);
}
```

职责分离：

| 指纹 | 服务于 | 含 |
|------|--------|-----|
| `OptionsHash`（既有） | `Verify`（重部署门） | FastMode/ResolveDependency/FullCheckMode 开关 |
| `PriorityHash`（新） | `Verify`（重部署门） | `Setup.Source` + `Setup.SourceOrders` |
| `platformChanged` | `SyncPackages`（floating 重解析门） | 仅 platform + BaseLock==null |

```csharp
// LockDataExtensions.cs:Verify —— 增 priorityHash 参数（caller 用 ViabilityHashHelper 算好传入）
public static bool Verify(this LockData self, Profile.Rice setup, string optionsHash, string priorityHash)
{
    if (self.Platform.Minecraft != setup.Version || self.Platform.Loader != setup.Loader) return false;
    if (self.Viability.OptionsHash != optionsHash) return false;
    if (self.Viability.PriorityHash != priorityHash) return false;   // 新增
    var setupPurls = setup.Packages.Where(x => x.Enabled).Select(x => x.Purl).ToHashSet();
    var lockPurls = self.Packages.Select(x => x.Purl).ToHashSet();
    return setupPurls.SetEquals(lockPurls);
}
```

`platformChanged` 已只看 platform（`SyncPackagesStage.cs` NOTE 注释），无需本任务再动。

> **FORMAT 保持 2**：recipe 全链路未发版，无存量锁需保护。给 `ViabilityData`/`LockedPackage` 加带默认值的字段就地落，不发版就不存在 break change。dev 期间旧构建的锁被新构建读到时，缺省字段反序列化为 `null`（**不抛 `JsonException`**），`Verify` 因 `PriorityHash` 不匹配而失败 → 触发一次全量重建，无害。

### 3.9 `PackageConflictException`（同档不可决议冲突）

```csharp
// TridentCore.Core/Exceptions/PackageConflictException.cs（新文件）
public class PackageConflictException(string subject, IReadOnlyList<LockData.LockedPackage> collisions)
    : Exception(
        $"Unresolvable package conflict on {subject}: "
        + $"{collisions.Count} packages share the top priority — "
        + $"{string.Join(", ", collisions.Select(c => $"{c.Purl} [{c.Source ?? "manual"}]"))}. "
        + $"Reorder them in SourceOrders or remove duplicates.")
{
    public string Subject { get; } = subject;
    public IReadOnlyList<LockData.LockedPackage> Collisions { get; } = collisions;
}
```

抛出后 stage 抛异常 → `DeployTracker` 进 `Faulted` → 用户看到清晰错误。跨档冲突总是可决议、内部 suppress、不阻断；只有同档平手（source 无法排序）走到这里阻断。

### 3.10 Profile 数据"已分组"的含义

部署读 `Entry.Source` + `Setup.Source` + `Setup.SourceOrders` 现场判定覆盖力，Profile 维持扁平 `IList<Entry>`。分组是 `Source` 派生视图，不是物理结构。

---

## 4. 改动面

### Trident（submodules/Trident.Net）—— 新增/改代码见 §3

| 层 | 文件 | 改动 |
|----|------|------|
| 锁结构 | `Abstractions/FileModels/LockData.cs` | `ViabilityData`+`PriorityHash`；`LockedPackage` 改 `Resolved` 为全量 `Package` + `SuppressedBy`；删 `ResolvedPackage`/`Compatibility`/`FileHashes`（FORMAT 不变） |
| FastMode 门 | `Abstractions/Extensions/LockDataExtensions.cs:14-30` | `Verify` 增 `PriorityHash` 比较（§3.8） |
| Profile | `Abstractions/FileModels/Profile.cs` (`Rice`) | 加 `IList<string> SourceOrders = []` |
| 新增异常 | `Core/Exceptions/PackageConflictException.cs` | 同档平手 throw → Faulted，唯一外漏信号（§3.9）——与其余异常同归 `Core.Exceptions` |
| 新增 helper | `Core/Utilities/ViabilityHashHelper.cs` | `PriorityOf(setup)` + `OptionsOf(options)`（§3.8） |
| 新增 helper | `Core/Utilities/PackagePathHelper.cs` + `Core/Extensions/LockDataExtensions.cs` | `RelativeTarget(...)` 唯一公式 + `LockedPackage.RelativeTarget()` 扩展（§3.6） |
| 新增 stage | `Core/Engines/Deploying/Stages/FlattenPackagesStage.cs` | §3.5 全文 |
| **身份键改造** | `Core/Engines/Deploying/Stages/SyncPackagesStage.cs` | `Key`+`Source`；matched 重建带 `SuppressedBy = null`（§3.3） |
| resolve | `Core/Engines/Deploying/PackagePlanner.cs` | `ResolveAsync` `ToDictionary`→`ToLookup`+`SelectMany`、请求 `.Distinct()`（§3.4）；删 `ReconstructPackage`/`RecomputeRule`（规则复算直接 `EvaluateRule`，§3.6） |
| 清单 | `Core/Engines/Deploying/Stages/GenerateManifestStage.cs:46-48,112-121` | 跳过 `SuppressedBy`；删 `ComputeRelativeTarget` 改用 `locked.RelativeTarget()` |
| pipeline | `Core/Engines/DeployEngine.cs:38-48` | `Sequence` 加 `typeof(FlattenPackagesStage)`（插在 SyncPackages 后） |
| stage 枚举 | `Core/Engines/Deploying/DeployStage.cs` | 加 `FlattenPackages`；`ResolvePackage` → `SyncPackages`（与 stage 类名对齐） |
| 上下文 | `Core/Engines/Deploying/DeployContext.cs:6,28` | 加 `string priorityHash` 构造参数 + `PriorityHash` 属性 |
| 引擎 | `Core/Engines/DeployEngine.cs:5,40` | 透传 `priorityHash` |
| 加载 | `Core/Engines/Deploying/Stages/LoadLockStage.cs:60` | `Viability = new(Context.OptionsHash, Context.PriorityHash)` |
| 持久化 | `Core/Engines/Deploying/Stages/PersistLockStage.cs` | **无需改动**——原样序列化 `Context.Lock`，`PriorityHash` 随 LoadLock 写入自然落盘 |
| 入口 | `Core/Services/InstanceManager.cs` | 传 `ViabilityHashHelper.OptionsOf(deploy)`/`PriorityOf(profile.Setup)` 给 Verify；stage-switch 加 `case FlattenPackagesStage`（只推进 StageStream，仲裁无运行时报告） |

> `DeployStage.ResolvePackage` 已对齐为 `SyncPackages`（与 `SyncPackagesStage` 一致）；resx 键 `DeployStage_SyncPackages`、值改为 "Synchronizing packages..." / "同步包..."（概念从"解析"转为"同步"）。

### Polymerium（src/Polymerium.Avalonia）

| 层 | 文件 | 改动 |
|----|------|------|
| 状态聚合 | `Services/InstanceStateAggregator.cs` | **无需改动**——仲裁无运行时报告，UI 读锁的 `SuppressedBy` 还原冲突（归 POLY-119） |
| stage 名 | `PageModels/InstanceHomePageModel.cs` + `Properties/Resources.resx`/`.zh-hans.resx`/`Designer.cs` | `FlattenPackages` 展示名（三份 resx 同步） |
| 冲突错误文案 | —— | **无需改动**——Polymerium 无异常→本地化串映射层，所有 Trident 异常正文均走 `ex.Message`；`PackageConflictException` 与既有异常一致 |
| UI 渲染 | —— | **本任务不做**，归 POLY-119 |

> `Utilities/PackageSourceHelper.cs`（POLY-118 产物）不改——覆盖顺序已完全由 Trident 用 `Setup.Source`+`SourceOrders` 计算；`PackageSourceHelper.Classify` 只服务 UI 分组标签。

---

## 5. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 整合包 + 手动同 mod（同路径） | 手动(3)落盘，整合包版 `SuppressedBy=手动purl`；UI 可警告 |
| 2 | recipe + 手动同 mod | 手动胜，recipe 版 `SuppressedBy` 指向手动 |
| 3 | 整合包 + recipe 同 mod（`SourceOrders` 空） | recipe(1) > 整合包(0)，recipe 胜 |
| 4 | 两 recipe 同 mod（都在 `SourceOrders`） | 列表后者胜（覆盖前者） |
| 5 | 无冲突 | 行为零变化，`SuppressedBy` 全 null，无报告 |
| 6 | 跨档冲突部署 | 不报错，正常启动，仅 UI 警告 |
| 7 | 改 `SourceOrders`/`Setup.Source` 后启动 | FastMode 失效（`PriorityHash` 变）→ 胜负翻转；floating 不重解析 |
| 8 | 基底 recipe 用例 | 整合包入列、recipe 不入 → 整合包旧版本胜（project 遍裁决，与 Normalizing 无关），recipe 版 `SuppressedBy` 指向整合包 |
| 9 | 败方锁定版本 | 留 `Lock.Packages`（`SuppressedBy` 非 null）不落盘；胜者被删后下次部署败者升为胜者、复用锁定版本无需重解析 |
| 10 | 不同 mod 同名 jar（路径撞，跨档） | path 遍在 project 幸存者里按覆盖顺序解决，列为冲突 |
| 11 | **同 mod 同版本不同来源**（如整合包+手动都 JEI 1.20.1） | `ResolveAsync` 只 resolve 一次、结果分发两条；Flatten 按覆盖顺序解决 |
| 12 | **同档平手**（两手动同路径 / 两个未入列 recipe 撞 / 同组重复） | 抛 `PackageConflictException`，deploy `Faulted`，错误指明撞的包与来源 |
| 13 | FORMAT | 保持 2；旧 dev 构建的锁读到顶多全量重建一次 |

---

## 6. 风险与取舍

| 风险 | 取舍 |
|------|------|
| `SyncPackages` 是 bug 高发区，改身份键 | 键细化是结构性的，matched/added/removed 逻辑不变；fixed→floating 继承不破坏（`(project,source)` 不随 vid 变）。无测试项目，靠构建 + 人工推理验证 |
| `ResolveAsync` 改 `ToLookup` 影响 exporter 宿主路径 | `ResolveAsync` 是底层共用方法；改后语义"resolve 一次、分发多条"对单条调用方等价（单条 lookup 退化为单元素） |
| 同档冲突阻断部署可能卡住存量实例 | recipe 未发版无存量用户；同档冲突本就是 setup 错误，报错比静默拍板正确 |
| `SuppressedBy` 破坏"锁只记最终状态"惯例 | scoped 诊断字段，正当因为"谁覆盖谁"是部署结果核心事实；不扩成塞诊断的先例 |
| 新增 stage 牵动 4 处接线 | Sequence + DeployStage 枚举 + InstanceManager switch + InstanceHomePageModel/resx；可接受，换职责清晰 |
| `HashHelper` 对 `IList<string>` 哈希行为未知 | `ViabilityHashHelper.PriorityOf` 用 `string.Join('\n', SourceOrders)` 显式展平，保证内容+顺序参与哈希 |

---

## 7. 不做的事（明确边界）

- 不支持多整合包（`Setup.Source` 单值）。
- 不改部署路径规则。
- 不自动找替代版本。
- 跨档冲突不阻断部署（仅 UI 警告）；同档冲突阻断报错。
- 不把分组持久化成 Profile 嵌套。
- 不让 Trident 认识 recipe。
- 不做冲突 UI 渲染（POLY-119）。
- 不扩 `OptionsHash`（覆盖顺序走独立 `PriorityHash`）。
- 不升 FORMAT。
- 不修 `FastMode` 入 `OptionsHash` 的 smell。
- 不对同档冲突搞隐藏 tiebreak。

---

## 附录：备选方案备案

### E.1 冲突解决的位置

当前选：**独立 `FlattenPackages` stage**（§3.5）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 合进 `SyncPackages` 尾 pass | 不增 stage | `SyncPackages` 已承担身份键改造（主体工作），再叠冲突解决职责变杂；独立 stage 让版本对账与"决定落盘"职责分明，消费同类型 `LockedPackage` 零数据摩擦 |
| 折进 GenerateManifest | 那里现算 target | 在 PersistLock 之后，败者 `SuppressedBy` 进不了锁；且混入清单生成 |
| 折进 SolidifyManifest 的 UpsertProjection | 改包优先级 | 在 PersistLock 之后同上；且会下载败者（浪费带宽） |

### E.2 败方的处理

当前选：**留 `Lock.Packages` + `SuppressedBy` 指向胜者**（§3.6）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 败方不进锁 | 仅胜者进 | 覆盖顺序调整时败者被当 Added → 重解析（网络），违"锁定不漂移" |
| `bool Suppressed`（不带胜者） | 二值 | 锁不自描述"被谁覆盖"；选 `SuppressedBy` 开诊断口子 |
| 败方落盘到 `mods/.disabled/` | 备用目录 | 改变 MC 加载语义；启停已有 `Enabled`，不混用 |

### E.3 FastMode 与覆盖顺序变更的耦合

当前选：**独立 `PriorityHash`（只进 `Verify`）**（§3.8）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 扩大 `OptionsHash` | 一份指纹 | `platformChanged` 用 `OptionsHash` 判定，扩进去让覆盖顺序变更误触发 floating 重解析 |
| 不做 | 接受 FastMode 命中时不重算 | 改 `SourceOrders` 后 build/ 仍是旧胜者软链，违目标 5 |

### E.4 同档冲突的处理

当前选：**报错阻断（`PackageConflictException`）**（§3.5/§3.9）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 字符串 tiebreak（source/purl 升序取 max） | 静默拍板 | 同档平手是 setup 错误，静默拍板是隐藏行为，掩盖问题；要求用户用 `SourceOrders` 排序或删重复才是正确导向 |
| warn 不阻断 + 静默拍板 | 同上加警告 | 仍保留了"拍板"这一隐藏行为；不如直接报错让用户修 |

### E.5 SourceOrders 的范围与方向

当前选：**通用来源排序（整合包可入列），末个 = 最高（追加即覆盖）**

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| `RecipeOrder`（仅 recipe） | 整合包恒底 | 无法表达基底 recipe 用例（整合包旧版本压过 recipe 最新） |
| 首个 = 最高 | 拖到顶 = 胜 | 与"覆盖顺序/图层栈"语义相左；列表应读作应用序列 |
