# Source / Reference 语义拆分

> 制定日期：2026-06-29
> 定位：核心架构任务，是 Recipe 系统的前置条件，也是 PACKAGE-GROUPING-UI 和 DEPLOYMENT-PRIORITY-BY-GROUP 的语义基础。
> 当前状态：**未实施**。本文档是未来实施的蓝本，据此编码不需重新调研。
> Jira：[POLY-117](https://d3ara1n.atlassian.net/browse/POLY-117)
> 依赖：[POLY-115](URL-SCHEME-UNIFICATION.md)（recipe:// 形式约定）。

---

## 0. 本任务在 Recipe 系统中的位置

> Recipe 系统总纲：[RECIPE-SYSTEM.md](RECIPE-SYSTEM.md)

Recipe 引入后，一个实例里会同时存在三种来源的 mod：整合包带来的、recipe 带来的、用户手动加的。要正确分组、锁定、部署优先级，必须先有一个**清晰的"归属"语义模型**。

当前 `Source` 字段一身二任（既是每个包的来源标记，又是实例级单整合包引用），且 `IsLocked` 写死"单来源"假设。本任务把这两重含义拆开：`Entry.Source` 表达"这个包从哪来"（`!= null` 即锁定），实例级 `Reference` 表达"本实例绑定了哪个整合包"（单值）。这是 Recipe、分组 UI、部署优先级三者的共同语义地基。

---

## 1. 背景与动机

### 1.1 现状：Source 一身二任

`Profile.Rice.Source`（实例级，`FileModels/Profile.cs:31`）和 `Profile.Rice.Entry.Source`（包级，`Profile.cs:43`）**同名、同类型（`string?`）、同语义域，但承担两件不同的事**：

| 字段 | 当前承担的职责 |
|------|----------------|
| `Profile.Rice.Source`（实例级） | ① 标识本实例从哪个整合包导入；② 作为 `IsLocked` 判断的基准值 |
| `Entry.Source`（包级） | ① 标识该包来自哪个整合包（导入时设为整合包 purl）；② 与实例级 Source 比较得出锁定 |

**整合包导入时的赋值**（`CurseForgeImporter.cs:38,48`、`ModrinthImporter.cs:39,56`）：

```csharp
var source = pack.Reference is not null ? PackageHelper.ToPurl(pack.Reference) : null;
Setup.Source = source;                 // 实例级
// 每个 Entry:
Source = source,                       // 包级，恰好 == 实例级 Source
```

所以"整合包带来的包"判定为 `entry.Source == profile.Setup.Source`——这正是 `IsLocked` 的实现。

### 1.2 问题：单来源假设撞上多来源现实

`IsLocked` 的现行实现（`InstanceSetupPageModel.cs:136`）：

```csharp
x.Source is not null && x.Source == Basic.Source
```

这蕴含一个强假设：**一个实例只有一个整合包来源**。Recipe 引入后这个假设破裂：

| 场景 | 现行 `IsLocked` 行为 | 问题 |
|------|---------------------|------|
| 实例挂了整合包 + 用户加 recipe | recipe 包 `Source = recipe://xxx`，与 `Basic.Source`（整合包 purl）不等 → **不锁** | recipe 包本应锁定，却被当手动包 |
| 实例只挂 recipe（无整合包） | `Basic.Source = null` → 所有包 `Source == null` 恒假 → **全不锁** | recipe 包本应锁定，全错 |
| 两个 recipe | recipe 包之间无法用单值 `Basic.Source` 区分 | 分组与锁定都失效 |

### 1.3 Source 的 14 个读取点（调研汇总）

以下每处都依赖"Source 是 purl 文本 / 单来源假设"，需在拆分后逐一复核：

**IsLocked / 锁定判断**
- `InstanceSetupPageModel.cs:136` — `x.Source is not null && x.Source == Basic.Source`（核心）
- `InstanceSetupPageModel.cs:202` — `model.IsLocked = false`（解锁后）
- `InstancePackageModel.cs:32` — `IsLocked` 字段，值由外部传入
- `PackageExplorerPageModel.cs:187,364` — `installed.Source == null || installed.Source != Basic.Source` → 可编辑

**实例级 Source 用于 UI**
- `InstanceBasicModel.cs:39` — `OnSourceChanged`，`PackageHelper.TryParse` 解析出 label 显示
- `InstanceSetupPageModel.cs:168-187` — 构造 `InstanceReferenceModel` 展示来源
- `InstanceSetupPageModel.cs:659` — `ViewDetails`，`TryParse` 后查详情
- `MainWindowContext.cs:560,579,587,607,688` — 构造/判断 Source
- `InstanceWorkspacePageModel.cs:65` — `Basic.Source is not null`

**文件 sourced 判断**
- `InstanceFilesPageModel.cs:146,177,230,273` — `Basic.Source != null` 标记文件来源

**ProfileManager.Update 的 Source 匹配**
- `ProfileManager.cs:159` — `x.Source == handle.Value.Setup.Source`（圈定整合包范围）
- `ProfileManager.cs:167,199` — 写回 Source

**解锁操作**
- `InstancePropertiesPageModel.cs:311` — `_owned.Value.Setup.Source = null`
- `InstanceOperation.cs:93`（Trident Cli）— `guard.Value.Setup.Source = null`
- `TridentExporter.cs:38,41` / `TridentImporter.cs:58,61` — 导出/导入时置空 Source

---

## 2. 目标与非目标

**目标**

1. **语义拆分**：`Entry.Source`（包级来源）与实例级 `Reference`（整合包引用）各自独立。
2. **统一锁定判定**：`Entry.Source != null` 即锁定，不再依赖与实例级字段比较。
3. **支持多来源共存**：整合包 + recipe + 手动在同一实例内互不干扰。
4. **分组语义明确**：手动组（`Source == null`）/ 整合包组（`Source == Reference`）/ recipe 组（`Source` 是 `recipe://`）三类清晰。
5. **可控迁移**：现有 `profile.json` 平滑迁移到新字段名，旧数据不丢。

**非目标（本次不做）**

- 不实现 recipe 导入/管理（Recipe 系统任务）。
- 不改部署优先级（DEPLOYMENT-PRIORITY-BY-GROUP 任务），但本任务的 `Source` 分类是优先级机制的输入。
- 不改 UI 分组渲染（PACKAGE-GROUPING-UI 任务），但本任务定义分组依据。
- 不支持一个实例挂多个整合包——`Reference` 单值，整合包本质带副作用，叠加是错误实践（已在前期讨论否决）。

---

## 3. 核心设计

### 3.1 字段重定义

| 字段 | 位置 | 旧语义 | 新语义 |
|------|------|--------|--------|
| `Reference`（由 `Source` 改名） | `Profile.Rice`（实例级） | 实例来源 + IsLocked 基准 | **仅**整合包引用，单值。null = 非整合包实例 |
| `Source` | `Profile.Rice.Entry`（包级） | 与实例级比较得锁定 | 包来源标记，**`!= null` 即锁定** |

`Profile.Rice` 调整：

```csharp
public class Rice
{
    public string? Reference { get; set; }   // 由 Source 改名：整合包引用，单值
    public required string Version { get; set; }
    public string? Loader { get; set; }
    public IList<Entry> Packages { get; init; } = [];
    public IList<Rule> Rules { get; init; } = [];
}

public class Entry
{
    public required string Purl { get; set; }
    public required bool Enabled { get; set; }
    public string? Source { get; set; }        // 保留，语义：来源标记，!= null 即锁定
    public IList<string> Tags { get; init; } = [];
}
```

### 3.2 三类来源的判定规则

一个包的归属由 `Entry.Source` 唯一决定，与 `Reference` 配合得出分组：

```
if (entry.Source is null)
    → 手动添加（未锁定，可自由编辑/删除）
else if (entry.Source == profile.Reference)
    → 整合包带来（锁定，整体管理）
else if (InternalUri.IsKind(entry.Source, "recipe"))   // 依赖 URL-SCHEME-UNIFICATION
    → recipe 带来（锁定，整体管理）
else
    → 其他（理论上不出现；防御性当作锁定处理）
```

整合包导入时的赋值不变：`Setup.Reference = packPurl`，每个 `entry.Source = packPurl`，于是 `entry.Source == Reference` 天然成立。

recipe 导入（未来）：`entry.Source = InternalUri.Recipe(recipeId)`，不碰 `Reference`。

### 3.3 锁定语义的新定义

```
IsLocked(entry) = entry.Source is not null
```

不再与实例级字段比较。整合包和 recipe 带来的包都锁，手动加的不锁。这与"导入即锁定、不可单独删"的产品语义一致。

**解锁**操作的含义收窄为"解除整合包绑定"：把 `Reference` 置 null **并** 把所有 `entry.Source == 旧Reference` 的包 `Source` 置 null（它们变成手动包）。这与现状 `Setup.Source = null` 的效果一致，只是字段改名 + 显式连带清理包级 Source。

### 3.4 ProfileManager.Update 的范围圈定

现行（`ProfileManager.cs:159`）：

```csharp
x.Source == handle.Value.Setup.Source   // 整合包更新时只动整合包带来的包
```

改造为：

```csharp
x.Source == handle.Value.Setup.Reference
```

语义不变（圈定整合包范围），只是基准字段改名。recipe 的"更新"是不同机制（重新链接 recipe，由 Recipe 系统处理），不走 `ProfileManager.Update`。

---

## 4. 迁移与兼容

### 4.1 profile.json 字段迁移

`Profile.Rice.Source` → `Reference` 涉及 JSON key 变更。策略：

- 序列化用新 key `"reference"`。
- 反序列化**双读**：优先 `"reference"`，缺失则回退读 `"source"`（旧数据），读到则赋给 `Reference`。
- 首次加载旧 profile 并保存后，新 key 写入，旧 key 消失，自然迁移。

`Entry.Source` 的 JSON key 不变（仍 `"source"`），无需迁移。

### 4.2 IsLocked 行为变化的影响面

新规则 `Source != null` 比旧规则 `Source == Basic.Source` **锁定的包更多**（recipe 包从"不锁"变"锁"）。这是预期且正确的——recipe 包本就该锁。需复核的 UI 点：

- `PackageExplorerPageModel.cs:187,364`：可编辑判定。recipe 包将由"可编辑"变"锁定不可编辑"，符合产品意图。
- `InstanceSetupPageModel.cs:136,202`：IsLocked 计算 + 解锁。改为新规则。

### 4.3 已有 profile 的兼容性

现有 `profile.json` 中：
- 实例级 `source`（整合包 purl）→ 迁移为 `reference`，语义不变。
- 包级 `source`（== 实例级 source 的整合包 purl）→ 保持，`entry.Source == Reference` 仍成立。
- 手动包 `source: null` → 保持。

迁移后行为与现状一致，无破坏。

---

## 5. 改动面

| 层 | 文件 | 改动 |
|----|------|------|
| 数据模型 | `FileModels/Profile.cs` | `Rice.Source` → `Reference`；`Entry.Source` 保留（语义注释） |
| 序列化 | Profile 反序列化 | 双读 `reference` / 旧 `source`（§4.1） |
| 导入器 | `CurseForgeImporter.cs`、`ModrinthImporter.cs` | `Setup.Source = ...` → `Setup.Reference = ...`；`entry.Source = ...` 不变 |
| 导出器/CLI | `TridentExporter.cs`、`TridentImporter.cs`、`InstanceOperation.cs` | `Setup.Source = null` → `Setup.Reference = null` |
| 锁定判定 | `InstanceSetupPageModel.cs:136,202`、`InstancePackageModel.cs:32` | `IsLocked = entry.Source != null` |
| 可编辑判定 | `PackageExplorerPageModel.cs:187,364` | 用 `entry.Source != null` 判定锁定 |
| 来源展示 | `InstanceBasicModel.cs:39`、`InstanceSetupPageModel.cs:168-187,659`、`MainWindowContext.cs` 多处、`InstanceWorkspacePageModel.cs:65` | `Basic.Source` → `Basic.Reference`；`TryParse` 仍适用（Reference 仍是 purl） |
| 文件 sourced 判定 | `InstanceFilesPageModel.cs:146,177,230,273` | `Basic.Source != null` → `Basic.Reference != null` |
| Update 范围 | `ProfileManager.cs:159,167,199` | `Setup.Source` → `Setup.Reference` |
| 解锁 | `InstancePropertiesPageModel.cs:311` | `Setup.Reference = null` + 连带清理包级 Source |

---

## 6. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 旧 profile（`source` key）加载 | 实例级 Source 正确映射到 `Reference`，行为不变 |
| 2 | 加载后保存 | JSON 中变为 `"reference"`，旧 key 消失 |
| 3 | 整合包实例的包 | `IsLocked = true`（`Source == Reference != null`） |
| 4 | 手动加的包 | `IsLocked = false`（`Source == null`） |
| 5 | recipe 包（`Source = recipe://x`，未来） | `IsLocked = true`（即使 `Reference` 指向别的整合包） |
| 6 | 解锁整合包 | `Reference = null`，原整合包包 `Source` 置 null，变手动包 |
| 7 | `ProfileManager.Update` 更新整合包 | 只动 `Source == Reference` 的包，recipe/手动包不动 |
| 8 | recipe 包的 `Source` | 不被 `PackageHelper.TryParse` 当 Purl（依赖 URL-SCHEME-UNIFICATION 边界） |

---

## 7. 风险与取舍

| 风险 | 取舍 |
|------|------|
| 字段改名波及面广（14+ 读取点） | 改动机械但量大；逐点核对清单见 §1.3，编译期会暴露大部分遗漏（字段名不存在即报错） |
| 旧 profile 双读逻辑增加反序列化复杂度 | 一次性迁移成本，换取长期语义清晰；可在若干版本后移除回退读 |
| `IsLocked` 语义变严（recipe 包也锁）影响现有用户 | 现状下 recipe 包不存在，迁移期无实际用户受影响；规则统一后行为可预测 |
| `Reference` 与 `Entry.Source` 同为 `string?` 易混淆 | 命名差异（Reference vs Source）+ 本文档语义表区分；考虑加 XML doc 注明职责 |

---

## 8. 不做的事（明确边界）

- **不支持多整合包** —— `Reference` 单值，整合包叠加是错误实践。
- **不改分组 UI 渲染** —— 本任务只定义分组依据（§3.2），渲染归 PACKAGE-GROUPING-UI。
- **不改部署优先级** —— 本任务的来源分类是优先级输入，但优先级机制本身归 DEPLOYMENT-PRIORITY-BY-GROUP。
- **不实现 recipe 导入** —— recipe 的 `Source = recipe://` 赋值由 Recipe 系统任务实现，本任务只保证语义模型能接纳它。

---

## 附录：备选方案备案

### C.1 实例级字段命名

当前选：**`Reference`**（§3.1）

| 备选 | 做法 | 取舍 |
|------|------|------|
| 保留 `Source` 名，仅改语义 | 实例级仍叫 Source，但语义收窄为"整合包引用" | 与包级 `Entry.Source` 同名易混淆；用户已明确要求用 `Reference` 表达"链接"语义 |
| `ModpackReference` / `BoundModpack` | 更长更明确 | 啰嗦；`Reference` 在上下文中已足够清晰 |

### C.2 锁定判定的实现位置

当前选：**`IsLocked = entry.Source != null`**（调用方计算）

| 备选 | 做法 | 取舍 |
|------|------|------|
| `Entry` 上加计算属性 `IsLocked => Source != null` | 模型自带锁定判定 | `Entry` 在 Trident 抽象层，锁定是宿主语义（recipe 是 Polymerium 概念）；判定留在 Polymerium 层更合适 |
| `IsLocked` 服务/策略类注入 | 可配置锁定策略 | 过度设计，当前规则简单且固定 |

### C.3 解锁的连带清理

当前选：**解锁时连带把 `Source == 旧Reference` 的包级 Source 置 null**（§3.3）

| 备选 | 做法 | 取舍 |
|------|------|------|
| 仅置 `Reference = null`，包级 Source 不动 | 包变 `Source != Reference` 的"其他锁定包" | 语义混乱：这些包既不是手动也不是整合包；连带清理使其明确变为手动包 |
| 解锁时删除所有整合包 | 解锁 = 卸载整合包 | 与现状不符（现状解锁保留包为手动）；破坏性太大 |
