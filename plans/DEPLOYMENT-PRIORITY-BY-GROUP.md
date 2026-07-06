# 按分组部署优先级

> 制定日期：2026-06-29
> 定位：部署引擎任务，是 Recipe 系统的前置条件。允许同实例存在来源不同但目标冲突的包，按组优先级确定性解决冲突。
> 当前状态：**草案**。来源优先级模型成立，但冲突解决如何接入 POLY-116 重构后的 lock 结构（PersistLock 先于 GenerateManifest 的顺序矛盾、Suppressed 在双对象模型下的迁移）尚未定，不可照此施工。前置 POLY-116/118 已完成，待设计补全后转蓝本。
> Jira：[POLY-117](https://d3ara1n.atlassian.net/browse/POLY-117)
> 依赖：[POLY-116](https://d3ara1n.atlassian.net/browse/POLY-116)（✅ 已完成，冲突胜方的版本锁定）、[POLY-118](SOURCE-REFERENCE-SEMANTICS.md)（✅ 已完成，组归属判定）。
> NOTE: §3.3/§3.6/§4 的 lock 交互基于 POLY-116 重构前的结构（LockDataBuilder / LockedPackages 字典 / Parcel），实施时须按 notes/LockData.md 的新结构调整。关键矛盾：新 pipeline 中 PersistLock 在 GenerateManifest 之前，冲突解决（产出 Suppressed）须提前到 SyncPackages 与 PersistLock 之间新增 stage。

---

## 0. 本任务在 Recipe 系统中的位置

> Recipe 系统总纲：[RECIPE-SYSTEM.md](RECIPE-SYSTEM.md)

Recipe 让"一个实例同时挂整合包 + recipe + 手动包"成为常态。这些来源可能带来同一个 mod（例如整合包带了 JEI、用户又手动加了 JEI）。当前部署引擎对这种重复**没有任何优先级机制**——同路径先到先得、静默丢弃后者，谁赢取决于不可控的字典迭代顺序（见 §1.2）。

要让 recipe 真正可用，部署必须能**确定性**地按来源优先级解决冲突：手动加的覆盖 recipe 的、recipe 的覆盖整合包的，且用户在 UI 上能看到冲突警告。本任务建立这套机制。它与 PACKAGE-GROUPING-UI 的组排序同源——组的显示顺序就是部署的优先级顺序。

---

## 1. 背景与动机

### 1.1 现状：部署引擎无优先级概念

`SolidifyManifestStage`（`Engines/Deploying/Stages/SolidifyManifestStage.cs:34-47,205-228`）把所有包（FragileFile）用同一个优先级 `ProjectionPriority.Package = 0` 投影到 `build/` 目录：

```csharp
foreach (var fragile in manifest.FragileFiles)
    UpsertProjection(projections, fragile.TargetPath,
        ProjectionPriority.Package, fragile, $"package {fragile.TargetPath}");

private void UpsertProjection(... targetPath, ProjectionPriority priority, ...)
{
    if (projections.TryGetValue(targetPath, out var existing))
    {
        if (priority > existing.Priority)   // 0 > 0 = false → 跳过
            projections[targetPath] = new(...);
        else
            logger.LogDebug("... keeps ... over ...");   // 静默丢弃
        return;
    }
    projections[targetPath] = new(...);
}

private enum ProjectionPriority { Package = 0, Live = 1, Persist = 2 }
```

`ProjectionPriority` 的三个值（Package/Live/Persist）区分的是**文件类型**（包 vs live 目录文件 vs persist 目录文件），**不是包之间的优先级**。所有包共享 `Package = 0`。

### 1.2 问题：重复包的不确定行为

`LockDataBuilder.AddParcel`（`LockDataBuilder.cs:67-70`）是裸 `_parcels.Add(parcel)`，**不去重**（对比 `AddLibrary:72-103` 会去重）。`GenerateManifestStage`（`Stages/GenerateManifestStage.cs:45-60`）把每个 Parcel 原样转 FragileFile，**不按 Path 分组**。

于是当两个包算出相同 `TargetPath`（`PackagePlanner.cs:33-59`：路径是 `mods/{FileName}`，文件名来自仓库 API，同一 mod 同版本必然同名）时：

1. `LockData.Parcels` 里**两个 Parcel 并存**（锁文件记录了重复）。
2. `build/mods/jei-xxx.jar` 只建一个符号链接。
3. **谁赢取决于 `FragileFiles` 在 `UpsertProjection` 字典里的迭代顺序**——即 Profile 里 Entry 顺序 + LINQ 管道顺序，对用户完全不可见、不可控。

### 1.3 这正是 #73 与 recipe 的痛点

用户在 #73 描述："把两个小型整合包合并到一起……生怕误删了自己的补充模组"。现状下，合并后重复 mod 谁生效是黑盒。recipe 引入后会加剧：recipe 和手动包冲突时，用户手动加的本应胜出，但现状可能被整合包/recipe 的版本"静默覆盖"。必须建立确定性的、按来源的优先级。

---

## 2. 目标与非目标

**目标**

1. **允许重复**：同一 mod 可以来自不同 Source 共存于 Profile，不被部署引擎当错误。
2. **确定性优先级**：同目标 Path 冲突时，按来源组优先级解决——**整合包 < recipe < 手动**，高优先级覆盖低优先级。
3. **冲突可见**：冲突在 UI 上警告（哪个包被哪个覆盖），部署不报错中断。
4. **锁定一致**（依赖 LOCKDATA-MODERNIZATION）：冲突胜方的版本进 `LockedPackages`，败方不进。
5. **优先级 = 组顺序**：与 PACKAGE-GROUPING-UI 的组排序同源，用户调整组顺序即调整部署优先级。

**非目标（本次不做）**

- 不允许同实例挂多个整合包（SOURCE-REFERENCE-SEMANTICS 已限定 `Reference` 单值）。
- 不实现 recipe 本身（Recipe 系统任务）。
- 不改包的部署路径算法（`PackagePlanner` 的 `mods/{FileName}` 规则不变）。
- 不在部署失败时自动选择替代版本——冲突解决是"选谁落盘"，不是"找替代"。

---

## 3. 核心设计

### 3.1 优先级模型

优先级来自包所属的**来源组**，组判定依据 SOURCE-REFERENCE-SEMANTICS：

| 来源组 | 判定 | 部署优先级 |
|--------|------|-----------|
| 整合包 | `Source == Reference` | 最低（0） |
| Recipe | `InternalUri.IsKind(Source, "recipe")` | 中（1..N，按 recipe 组顺序） |
| 手动 | `Source == null` | 最高 |

数值越大优先级越高，冲突时覆盖低优先级。这与 §1.1 的 `UpsertProjection` 的 `priority > existing.Priority` 方向一致——只需把单值 `Package` 升级为按组派生的数值。

### 3.2 冲突的定义

**冲突 = 两个或以上包计算出相同的 `TargetPath`**（即部署后会落到 `build/` 下同一文件路径）。这与现有 `UpsertProjection` 的 `targetPath` 字典键完全对齐，无需新定义。

> 不以"同 project identity"判冲突——同一 mod 不同版本（如 JEI 1.20.1 vs 1.20.4）文件名不同、路径不同，各落各的，不算冲突。只有"同路径"才算。

### 3.3 冲突解决流程

改造点集中在 `GenerateManifestStage` 与 `SolidifyManifestStage` 之间。新增一个**冲突解决步骤**（可作为一个新 Stage 或 `GenerateManifestStage` 的子步骤）：

```
输入: List<Parcel>（带 Source）, rules（算 Path）
1. 对每个 Parcel 算 RelativeTargetPath（PackagePlanner）
2. 按 TargetPath 分组 → 每组 ≥2 个即为冲突
3. 每组内按 §3.1 优先级排序，取最高优先级为"胜者"，其余为"败者"
4. 产出:
   - EffectiveParcels: 仅胜者（进 LockData.LockedPackages，建符号链接）
   - Conflicts: 冲突报告（败者信息 + 被谁覆盖），供 UI 警告
```

败者**不进 `LockedPackages`**（避免锁两个版本），但**保留在 `Profile.Entry` 里**（用户可见、可调整）。败者在部署中既不下载也不落盘，仅作为"被覆盖"的占位存在。

### 3.4 `ProjectionPriority` 升级

现有枚举保留文件类型维度，新增包内优先级：

```csharp
// 文件类型优先级（保留，跨类型）
Persist > Live > Package

// 包内来源优先级（新增，仅在 Package 类型内比较）
int SourcePriority(Entry entry) =>
    entry.Source == null            ? 100            // 手动
  : entry.Source == Reference        ? 0              // 整合包
  : InternalUri.IsKind(entry.Source, "recipe") ? (10 + recipeOrder)  // recipe
  : /*防御*/                          ? 100;
```

`UpsertProjection` 比较时，同属 `Package` 类型的两个候选，按 `SourcePriority` 决胜。这样最小改动现有投影逻辑——`Persist`/`Live` 仍稳压所有包。

> 优先级数值用"间隔刻度"（0/10+/100）而非连续整数，便于 recipe 组顺序插入而不重排其他。

### 3.5 冲突报告与 UI 警告

冲突报告随部署结果回传上层（`InstanceManager.Deploy` 的返回值或事件）：

```csharp
public record PackageConflict(
    string TargetPath,
    PackageConflictSide Winner,
    IReadOnlyList<PackageConflictSide> Losers);

public record PackageConflictSide(string Purl, string? Source, string DisplayName);
```

UI（PACKAGE-GROUPING-UI 的组视图）在对应包上显示警告标识（"被 X 覆盖"）。部署不因冲突报错——冲突是预期内状态，用户用 UI 调整（移除败方或改优先级）。

### 3.6 与 LockData 的交互（依赖 LOCKDATA-MODERNIZATION）

`LockedPackages` 以 project identity（`label:ns/pid`）为键。冲突的两个包 project identity 可能相同（同一 mod 不同来源）也可能不同（两个 mod 都想占 `mods/foo.jar`）：

| 情况 | 锁定 |
|------|------|
| 同 project 不同来源（整合包 JEI vs 手动 JEI） | 同键，胜者覆盖败者写入锁 |
| 不同 project 同路径（极少，如两个 mod 都叫 `foo.jar`） | 不同键各自可锁，但部署仅胜者落盘；败者在锁里标记 `Suppressed` |

败方进锁时加 `Suppressed` 标记而非删除——这样用户调整优先级后，原败方恢复时无需重新解析版本（保留其锁定版本）。`Suppressed` 包部署时不落盘。

### 3.7 Profile 数据"已分组"的含义

> 用户明确：Profile 里的数据"已经分好组"——指 `Entry.Source` 天然携带组归属，**不需要把 Profile 物理结构改成嵌套**。

部署引擎读 `Entry.Source` 现场判定组与优先级，Profile 维持扁平 `IList<Entry>`。这与 PACKAGE-GROUPING-UI 的 UI 层动态聚合一致——分组是 `Source` 维度的派生视图，不是物理结构。

---

## 4. 改动面

| 层 | 文件 | 改动 |
|----|------|------|
| 优先级 | `Engines/Deploying/PackagePlanner.cs` 或新 `SourcePriority.cs` | `SourcePriority(entry, Reference, recipeOrder)` |
| 投影 | `Stages/SolidifyManifestStage.cs` | `UpsertProjection` 包内决胜用 `SourcePriority` |
| 冲突解决 | `Stages/GenerateManifestStage.cs`（或新 Stage） | 按 TargetPath 分组检测冲突，产出 EffectiveParcels + Conflicts |
| 锁 | `LockDataBuilder.cs` / `LockData.cs`（依赖 LOCKDATA-MODERNIZATION） | `Parcel` 带 `Source`；败方 `Suppressed` 标记 |
| 上层 | `Services/InstanceManager.cs` | `Deploy` 返回值携带冲突报告；事件通知 UI |
| UI | `PACKAGE-GROUPING-UI` | 冲突包警告标识 |
| 优先级输入 | `Profile.Rice`（recipe 组顺序字段，与 PACKAGE-GROUPING-UI 共用） | recipe 组顺序持久化 |

---

## 5. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 整合包 + 手动同 mod（同路径） | 手动版落盘，整合包版被覆盖；UI 警告 |
| 2 | recipe + 手动同 mod | 手动版胜，recipe 版被覆盖 |
| 3 | 整合包 + recipe 同 mod（无手动） | recipe 版胜 |
| 4 | 两个 recipe 同 mod | recipe 组顺序靠后的胜（优先级高） |
| 5 | 无冲突 | 行为与现状一致，零警告 |
| 6 | 冲突部署 | 不报错，正常启动，仅 UI 警告 |
| 7 | 调整 recipe 组顺序后部署 | 胜负关系按新顺序翻转 |
| 8 | 冲突败方的锁定版本 | 进 LockData 但 `Suppressed`，不落盘；恢复优先级后复用锁定版本无需重解析 |
| 9 | 不同 mod 同名 jar（路径撞） | 按优先级解决，列为冲突 |

---

## 6. 风险与取舍

| 风险 | 取舍 |
|------|------|
| 冲突败方仍占 LockData 空间 | 标记 `Suppressed` 而非删除，换取优先级翻转时无需重解析；空间开销可忽略 |
| 用户不理解"被覆盖" | UI 警告明确显示"X 被 Y（手动/recipe/整合包）覆盖"，并提供跳转到败方所在组的入口 |
| 优先级数值刻度的可扩展性 | 用间隔刻度（0/10+/100），recipe 组顺序插入 10+ 区间，不挤占整合包/手动的固定档 |
| 现状"先到先得"用户的存量实例 | 新机制对存量实例是行为改进（更可预测）；无冲突的实例行为完全不变（场景 5） |

---

## 7. 不做的事（明确边界）

- **不支持多整合包** —— `Reference` 单值，整合包优先级固定最低。
- **不改部署路径规则** —— `mods/{FileName}` 不变，冲突是路径撞的自然结果。
- **不自动找替代版本** —— 冲突解决是"选已存在的谁落盘"，不是联网找新版本。
- **不因冲突中断部署** —— 冲突是预期状态，仅 UI 警告。
- **不把分组持久化成 Profile 嵌套结构** —— 分组是 `Source` 派生视图（§3.7）。

---

## 附录：备选方案备案

### E.1 优先级机制位置

当前选：**`UpsertProjection` 内按 `SourcePriority` 决胜**（§3.4）

| 备选 | 做法 | 取舍 |
|------|------|----------|
| `ResolvePackageStage` 提前去重 | 解析阶段就按优先级只保留胜者 | 败者版本信息丢失（无法记录锁定版本、无法 UI 显示）；解析阶段 Path 未定，无法判冲突 |
| 单独 ConflictResolution Stage | 在 GenerateManifest 后、Solidify 前 | 最清晰，但与 GenerateManifest 的 Path 计算重复；折中为 GenerateManifest 的子步骤 |

### E.2 冲突粒度

当前选：**同 `TargetPath`**（§3.2）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 同 project identity | 同 mod 任意版本算冲突 | 同 mod 不同版本路径不同、各落各的，不冲突；按 project 误报 |
| 同 FileName | 按文件名判 | 与 TargetPath 等价但更粗糙（忽略 Destination 规则）；用 Path 更精确 |

### E.3 败方的处理

当前选：**保留在 Profile + LockData 标 `Suppressed`，不落盘**（§3.3, §3.6）

| 备选 | 做法 | 取舍 |
|------|------|----------|
| 败方从 Profile 删除 | 冲突即删败者 | 破坏性大，用户失去败者信息；调整优先级也无法恢复，需重新加 |
| 败方落盘到备用目录 | 如 `mods/.disabled/` | 改变 MC 加载语义，引入复杂度；现状已用 `Enabled` 控制启停，不混用 |
| 败方不进 LockData | 仅胜者进锁 | 优先级翻转时败者需重新解析版本，与 LOCKDATA 锁定语义冲突；`Suppressed` 更优 |

### E.4 优先级数值方案

当前选：**间隔刻度（0 / 10+N / 100）**（§3.4）

| 备选 | 做法 | 取舍 |
|------|------|----------|
| 连续整数（0,1,2,...） | 按 entry 顺序 | recipe 组插入需重排所有数值；刻度间隔更灵活 |
| 按组顺序单一序列 | 用 PACKAGE-GROUPING-UI 的组顺序直接作优先级 | 整合包/手动位置固定，仅 recipe 可调；混合刻度 + recipe 子顺序更贴合 |
