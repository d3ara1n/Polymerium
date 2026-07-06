# Source 归属与锁定语义

> 制定日期：2026-06-29
> 修订日期：2026-07-06（移除 Reference 改名方案，保留 Source；拆分 IsLocked 为 CanRemove / CanUpdate；引入 PackageSourceHelper 投影模型）
> 定位：Recipe 系统的语义地基——定义 Source 的四种归属，把包级 IsLocked 拆成 CanRemove / CanUpdate，为分组 UI、部署优先级、Exhibit 提供统一的分类与锁定契约。
> 当前状态：蓝本，✅ 已实施（2026-07-06）。下游任务直接消费 PackageSourceHelper（Classify / CanRemove / CanUpdate / CanUngroup）。
> Jira：[POLY-118](https://d3ara1n.atlassian.net/browse/POLY-118)
> 依赖：[POLY-115](URL-SCHEME-UNIFICATION.md)（recipe:// 形式与 InternalUriHelper 判定工具，**必须先落地**）

---

## 0. 本任务在 Recipe 系统中的位置

> Recipe 系统总纲：[RECIPE-SYSTEM.md](RECIPE-SYSTEM.md)

Recipe 引入后，一个实例里会同时存在三种来源的包：整合包带来的、recipe 带来的、用户手动加的。要正确分组、锁定、决定部署优先级，必须先有一个清晰的"归属 + 锁定"语义模型。

本任务交付这个模型：把 `Entry.Source` 的可能取值归类为四种归属，把现行单一的 `IsLocked`（同时管"能不能删"和"能不能改版本"）拆成两个正交属性，并定义"解绑整合包"与"解散分组（Ungroup）"两个动作的关系。分组 UI（PACKAGE-GROUPING-UI）和部署优先级（DEPLOYMENT-PRIORITY-BY-GROUP）都消费本任务产出的分类契约。

---

## 1. 背景与动机

### 1.1 现状：IsLocked 一身二任

`Profile.Rice.Entry.Source`（`FileModels/Profile.cs:43`，包级）标记"这个包从哪来"：
- `null` —— 用户手动添加
- 整合包 purl（由 `CurseForgeImporter.cs:43` / `ModrinthImporter.cs:47` 写入）—— 整合包带来

实例级 `Profile.Rice.Source`（`FileModels/Profile.cs:31`）标记"本实例绑定了哪个整合包"，单值。导入时 `entry.Source` 与实例级 `Setup.Source` 同时被赋为整合包 purl，因此 `entry.Source == profile.Setup.Source` 天然成立。

包级 `InstancePackageModel.IsLocked`（`Models/InstancePackageModel.cs:32`）现行实现（`InstanceSetupPageModel.cs:136`）：

```csharp
x.Source is not null && x.Source == Basic.Source
```

这一个布尔同时承担两件事：① 不能删；② 不能改版本。它蕴含"一个实例只有一个整合包来源"的假设。

### 1.2 问题：单源假设撞上多来源现实

Recipe 引入后该假设破裂：

| 场景 | 现行 IsLocked 行为 | 问题 |
|------|---------------------|------|
| 实例挂整合包 + 加 recipe | recipe 包 `Source = recipe://x`，与 `Basic.Source` 不等 → 不锁 | recipe 包本应不能单独删，却被当手动包 |
| 实例只挂 recipe（无整合包） | `Basic.Source = null` → 所有包恒不锁 | recipe 包应锁（不能单独删），全错 |
| 两个 recipe | 单值 `Basic.Source` 无法区分 | 分组与锁定都失效 |

更根本的问题：recipe 包和整合包包**都应"不能单独删"**（因为它们属于一个组，组是原子的），但 recipe 包**可以改版本**（recipe 不占有版本号），只有当前整合包包**连版本也不能改**（版本由整合包占有）。单一 `IsLocked` 表达不了这个区别。

### 1.3 受影响的读取点

本任务**不改字段名**（`Source` 保留），所以绝大多数读取来源文本的站点（来源展示、文件 sourced 判定、`ProfileManager.Update` 范围圈定）零改动。真正需要语义改动的是**消费包级 `IsLocked` 的站点**：

| 站点 | 现状 | 改动 |
|------|------|------|
| `Models/InstancePackageModel.cs:7,32` | 构造入参 `isLocked` + `[ObservableProperty] IsLocked` | 退役 `IsLocked`，改投影 `CanRemove` + `CanUpdate`（持 Source + currentReference） |
| `PageModels/InstanceSetupPageModel.cs:136` | `new InstancePackageModel(x, x.Source is not null && x.Source == Basic.Source)` | 构造改投影，`isLocked` 入参退役 |
| `PageModels/InstanceSetupPageModel.cs:194-204` | `else` 分支逐个 `model.IsLocked = false` | 解绑后重算 CanUpdate（currentReference=null） |
| `PageModels/InstanceSetupPageModel.cs:515` | 筛选谓词 `x.IsLocked == it` | "锁定"筛选语义重定义（§3.6） |
| `PageModels/InstanceSetupPageModel.cs:803` | `!entry.IsLocked && ...` | 按该处真实意图改 `CanRemove` 或 `CanUpdate` |
| `Pages/InstanceSetupPage.axaml:104` | `IsEnabled="{Binding IsLocked, ...Not}"` | 重指向 `CanRemove`/`CanUpdate`（视该控件语义） |
| `PageModels/PackageExplorerPageModel.cs:187,364` | `installed.Source == null \|\| installed.Source != Basic.Source` | 表达式恰等价于 `CanUpdate`，换用 helper |

**明确不动**（与本任务的包级语义无关，仍由 `Basic.Source != null` / `Setup.Source` 驱动）：

- `PageModels/InstanceWorkspacePageModel.cs:65` —— 实例级 `IsLocked => Basic.Source is not null`（实例是否绑定整合包）。
- `PageModels/InstanceFilesPageModel.cs`（`:146,177,230,273,500,501,566,...`）与 `Models/FileAssetModel.cs:17` 等 —— 文件资产 `FileAssetModel.IsLocked`（文件是否来自整合包 overlay，无 recipe/legacy 维度）。
- `PageModels/InstancePropertiesPageModel.cs:311` —— 解绑 `_owned.Value.Setup.Source = null`。**行为已正确**：只清实例级、保留包级 Source，恰是新模型定义的"降级为 Legacy"；零改动，仅语义被显式化。

---

## 2. 目标 / 非目标 / 不做的事

**目标**

1. 定义 `Entry.Source` 的四种归属（Manual / Modpack / Recipe / Legacy），作为分组、锁定、优先级的共同输入。
2. 把包级 `IsLocked` 拆成 `CanRemove`（能否单独删）+ `CanUpdate`（能否改版本），外加组级 `CanUngroup`（整组能否解散）。
3. 锁属性**不作为状态存储**，全部是 Source（+ 当前 `profile.Setup.Source`）的纯函数，由 `PackageSourceHelper` 提供；`PackageModel` 与 Group VM 都只是投影。
4. 厘清"解绑整合包"（降级为 Legacy 组，保留来源）与"Ungroup"（整组清 Source 变手动）两个动作的关系。
5. 让 recipe 分支在数据上就绪（`Source = recipe://x` 可被正确分类）；recipe 的导入/管理仍归 Recipe 系统任务。

**非目标**

- 不实现 recipe 的导入/管理/UI —— Recipe 系统任务（POLY-120）。
- 不改分组 UI 的渲染 —— PACKAGE-GROUPING-UI；本任务只交付分组依据。
- 不改部署优先级机制 —— DEPLOYMENT-PRIORITY-BY-GROUP；本任务的归属分类是它的输入。

**不做的事**

- **不改 `Source` → `Reference`** —— 实例级与包级都保留 `Source`。改名是纯美观诉求（解决不了 bug），代价是 JSON schema 破坏 + 大面积改动；两者本就同源（导入时 `entry.Source == profile.Setup.Source`），同名如实反映这层关系（附录 C.1）。
- **不在解绑时清包级 Source** —— 解绑只清实例级 `profile.Setup.Source`，包级 Source 保留，分组信息不丢（附录 C.4）。
- **不支持一个实例挂多个整合包** —— `profile.Setup.Source` 单值；整合包带副作用，叠加是错误实践。
- **不动文件资产 / 实例级 IsLocked** —— 那些由 `Basic.Source != null` 驱动，语义不同，不在范围。

---

## 3. 核心设计

### 3.1 Source 的四种归属

一个包的归属由 `Entry.Source` 唯一决定，配合当前实例引用 `profile.Setup.Source` 得出分类：

| Source 取值 | 归属 | CanRemove | CanUpdate | 组可 Ungroup | 含义 |
|-------------|------|:-:|:-:|:-:|------|
| `null` | Manual | ✅ | ✅ | — | 手动添加，自由包 |
| `== profile.Setup.Source` | Modpack | ❌ | ❌ | ❌ | 当前整合包带来，版本被整合包占有 |
| `recipe://...`（`InternalUriHelper.IsKind(,"recipe")`） | Recipe | ❌ | ✅ | ✅ | recipe 带来，锁组但不占版本 |
| 非 null 且 `!= profile.Setup.Source` | Legacy | ❌ | ✅ | ✅ | 曾属某整合包，已解绑，保留来源标签 |

关键观察：**Recipe 与 Legacy 的 (CanRemove, CanUpdate) 完全相同**，区别只在 UI 来源标签——它们行为同构。Modpack 是唯一"连版本都锁"的归属。

### 3.2 PackageSourceHelper

新增 `src/Polymerium.Avalonia/Utilities/PackageSourceHelper.cs`，静态工具类（`Helper` 后缀，不可实例化），收敛全部归属与锁定判定：

```csharp
public static class PackageSourceHelper
{
    // 归属分类：Classify 的产出，分组 UI / 部署优先级 / Exhibit 的共同输入
    public enum Kind { Manual, Modpack, Recipe, Legacy }

    public static Kind Classify(string? source, string? current) =>
        source is null                       ? Kind.Manual
      : source == current                    ? Kind.Modpack
      : InternalUriHelper.IsKind(source, "recipe") ? Kind.Recipe
      :                                       Kind.Legacy;

    // 单包能否删除：只有手动包（不属任何组）可删
    public static bool CanRemove(string? source, string? current) => source is null;

    // 单包能否改版本：只有当前整合包占有版本，其余都可改
    public static bool CanUpdate(string? source, string? current) =>
        source is null || source != current;

    // 整组能否解散：当前整合包组不可（需先解绑降级），Recipe/Legacy 可
    public static bool CanUngroup(string? source, string? current) =>
        source is not null && source != current;
}
```

`current` 即 `profile.Setup.Source`。边界已验证：实例无整合包（`current=null`）时，手动包 `source=null` 经 `source is null` 短路得 `CanUpdate=true`，不会误锁。

工具放 Polymerium 层（不放 Trident）：锁定是宿主语义（recipe 是 Polymerium 概念），Trident 的 Purl 体系自洽。依赖 POLY-115 的 `InternalUriHelper`。

### 3.3 锁属性是投影，不是状态

`CanRemove` / `CanUpdate` 是 Source 的纯函数，**任何地方都不存储**：

- `InstancePackageModel` 暴露 `CanRemove` / `CanUpdate`，内部持包级 `Source` 与当前 `currentReference`，调用 `PackageSourceHelper` 计算。`CanRemove` 不依赖 currentReference（仅 `source is null`），构造后不变；`CanUpdate` 依赖 currentReference，Detach 时由页面重算。
- 未来的 Group VM（PACKAGE-GROUPING-UI）同样从组定义的 Source 投影——因为组成员由 Source 值定义、锁语义也由 Source 值决定，**一个组内所有成员的锁属性天然一致**，组的锁 = 任意成员的锁。
- ExhibitModel / PackageExplorerModel 对已安装包直接调 `PackageSourceHelper.Classify(...)`，无需 Group 对象。

这化解了"锁属性归 Group 还是 Package"的归属之争：两边都不持真相，都从同一份 Source 投影，没有同步问题。`IsLocked` 这个单值字段退役——它本在混用，拆完后没有等价单值（`!CanRemove` 是"归属某组"、`!CanUpdate` 是"版本被占"、`!CanRemove && !CanUpdate` 仅当前整合包，三者各是各的意思）。

### 3.4 解绑（Detach）与解散分组（Ungroup）

两个动作，先后分明，不冲突：

**解绑整合包（Detach）** —— `profile.Setup.Source = null`，包级 Source **不动**。
- 原 Modpack 组 → 降级为 Legacy 组：仍分组、UI 仍显示"来自整合包 XXX"、`CanUpdate` 从 false→true、`CanUngroup` 从 false→true。
- 现状 `InstancePropertiesPageModel.cs:311` 的 `_owned.Value.Setup.Source = null` **已是此行为**，零代码改动，只是语义被显式定义为"降级"而非"删除分组"。

**解散分组（Ungroup）** —— 整组操作，把组内所有包 `Source` 置 null，整组变 Manual。
- 仅 Recipe / Legacy 组可用（`CanUngroup`）。当前 Modpack 组不可直接 Ungroup——会与"仍绑定整合包"的事实冲突，必须先 Detach 降级为 Legacy。
- 组是原子的：不能对组内**单个**包 Ungroup，也不能增删单个成员；Ungroup 是全有或全无的整组操作。

用户想"把解绑后的整合包彻底摊平成手动包"：先 Detach（降级成 Legacy）→ 再 Ungroup（清 Source）。想保留来源标签就停在 Detach。

### 3.5 ProfileManager.Update 的范围圈定

`ProfileManager.cs:159` 用 `x.Source == handle.Value.Setup.Source` 圈定整合包更新时只动整合包带来的包。语义不变，字段名不变，**零改动**。recipe 的"更新"是不同机制（重新链接 recipe，由 Recipe 系统处理），不走 `ProfileManager.Update`。

### 3.6 "锁定"筛选器的语义重定义

`InstanceSetupPageModel.cs:515` 的 `x.IsLocked == it` 是包列表的"仅看锁定/未锁定"筛选。`IsLocked` 退役后，"锁定"的指向需重新选定：

- 选 `!CanRemove`（= 是否归属某组）—— 最贴近用户对"锁定包"的直觉（整合包/recipe/legacy 包都算锁定）。
- 选 `!CanUpdate`（= 是否版本被占）—— 范围窄，只含当前整合包包。

**建议选 `!CanRemove`**（"是否属于一个组"），与分组 UI 的分组概念一致。实施时确认。

---

## 4. 改动面

| 层 | 文件 | 改动 |
|----|------|------|
| 工具（新增） | `Utilities/PackageSourceHelper.cs` | 归属分类 + `CanRemove`/`CanUpdate`/`CanUngroup` |
| 包模型 | `Models/InstancePackageModel.cs:7,32` | 退役 `IsLocked`，改投影 `CanRemove` + `CanUpdate`（持 Source + currentReference） |
| 构造 | `PageModels/InstanceSetupPageModel.cs:136` | 构造 InstancePackageModel 改投影，`isLocked` 入参退役 |
| 解绑重算 | `PageModels/InstanceSetupPageModel.cs:194-204` | `model.IsLocked = false` → 重算 CanUpdate（currentReference=null） |
| 筛选 | `PageModels/InstanceSetupPageModel.cs:515` | 锁定筛选改 `!CanRemove`（§3.6） |
| 能力门 | `PageModels/InstanceSetupPageModel.cs:803` | `!entry.IsLocked` → 按意图改 `CanRemove`/`CanUpdate` |
| 绑定 | `Pages/InstanceSetupPage.axaml:104` | `IsLocked` 绑定重指向 `CanRemove`/`CanUpdate` |
| Explorer | `PageModels/PackageExplorerPageModel.cs:187,364` | 表达式换 `PackageSourceHelper.CanUpdate(...)` |

**不改动**（验证后确认）：`InstancePropertiesPageModel.cs:311`（解绑，行为已正确）、`ProfileManager.cs:159,199`（范围圈定，字段不变）、`InstanceWorkspacePageModel.cs:65`（实例级 IsLocked）、`InstanceFilesPageModel.cs` 全部（文件资产 IsLocked）、Importers / Exporters / CLI（字段不变）。

---

## 5. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 手动包（`Source = null`） | `CanRemove=true`，`CanUpdate=true` |
| 2 | 当前整合包包（`Source == profile.Setup.Source`） | `CanRemove=false`，`CanUpdate=false` |
| 3 | recipe 包（`Source = recipe://x`，未来） | `CanRemove=false`，`CanUpdate=true`（即使实例另绑整合包） |
| 4 | Legacy 包（`Source = 旧整合包 purl`，`profile.Setup.Source = null` 或指向别处） | `CanRemove=false`，`CanUpdate=true`，UI 仍显示"来自旧整合包" |
| 5 | 解绑整合包（Detach） | `profile.Setup.Source = null`，包级 Source 保留，原 Modpack 组降级为 Legacy（CanUpdate 变 true） |
| 6 | Ungroup 一个 Legacy 组 | 组内所有包 `Source = null`，整组变 Manual |
| 7 | 尝试 Ungroup 当前整合包组 | `CanUngroup=false`，操作不可用（须先 Detach） |
| 8 | 实例无整合包（`profile.Setup.Source = null`）时的手动包 | `CanUpdate=true`（不误锁） |
| 9 | `ProfileManager.Update` 更新整合包 | 只动 `Source == profile.Setup.Source` 的包，其余不动 |
| 10 | 旧 profile.json（`source` key）读取 | 行为不变（字段未改名，无迁移） |

---

## 6. 风险与取舍

| 风险 | 取舍 |
|------|------|
| `IsLocked` 退役波及多个消费点（XAML 绑定、筛选、能力门） | 拆分是正交清晰化；每个消费点按其真实意图重指向 CanRemove 或 CanUpdate，编译期 + 绑定报错暴露遗漏 |
| 投影模型要求 currentReference 变化时重算 CanUpdate | 仅 Detach 一处触发（`InstanceSetupPageModel.cs:194-204` 已是重算点），范围可控；CanRemove 不依赖 currentReference，无需重算 |
| recipe 分支依赖 POLY-115 的 InternalUriHelper | POLY-115 必须先落地；本任务的 Recipe 分支在 recipe 包出现（POLY-120）前不会被实际触发，但分类器需就位 |
| "锁定筛选器"语义从单值变二选一 | 选 `!CanRemove`（归属与否）最贴直觉；实施时与分组 UI 对齐确认 |

---

## 7. 不做的事（边界）

- **不改 Source 字段名** —— 保留 `Source`，不做 Reference 改名，不做 JSON 迁移（附录 C.1）。
- **不实现 recipe 导入** —— recipe 的 `Source = recipe://` 赋值归 Recipe 系统（POLY-120），本任务只保证分类器能接纳。
- **不改分组 UI 渲染** —— 本任务定义分组依据（§3.1），渲染归 PACKAGE-GROUPING-UI。
- **不改部署优先级机制** —— 归属分类是优先级输入，优先级本身归 DEPLOYMENT-PRIORITY-BY-GROUP。
- **不支持多整合包叠加** —— `profile.Setup.Source` 单值。
- **不动文件资产 / 实例级 IsLocked** —— 由 `Basic.Source != null` 驱动，语义不同。

---

## 附录：备选方案备案

### C.1 实例级与包级 Source 是否改名（如 Reference）

当前选：**保留 Source，不改名**

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 实例级改名 `Reference` + JSON 双读迁移 | profile.json `source`→`reference`，反序列化双读 | 改名是纯美观（不修 bug），代价是 JSON schema 破坏 + 大面积改动；两者同源（导入时 `entry.Source == profile.Setup.Source`），同名如实反映关系。已否决 |
| 仅包级改名 | `Entry.Source` → 别的 | 与实例级同名恰是同源体现；单独改包级制造不对称，无收益 |

### C.2 IsLocked 是否保留单值

当前选：**拆为 CanRemove + CanUpdate（+ 组级 CanUngroup）**

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| `IsLocked = Source != null` 单值 | recipe/legacy 也算锁 | 丢失"能否改版本"维度：recipe/legacy 包应可改版本，单值表达不了；Exhibit 版本下拉、筛选器都需要区分 |
| `IsLocked = Source == profile.Setup.Source`（现状） | 仅当前整合包锁 | recipe 包本应"不能单独删"，现状把它当手动包，错误 |

### C.3 锁属性存 Group 还是 Package

当前选：**都不存，纯函数投影（PackageSourceHelper）**

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 存 Group 上，Package 转发 | Group 持 CanRemove/CanUpdate | 第二份真相，须与 Source 同步；组成员由 Source 定义、锁也由 Source 决定，存起来纯属冗余 |
| 存 Package 上（现状模式，外部传入 bool） | 构造时算好传入 | currentReference 变化（Detach）时要逐个重算回填；不如让模型自持 currentReference 投影 |

### C.4 解绑是否连带清包级 Source

当前选：**不清，降级为 Legacy 组**

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 解绑时把 `Source == 旧引用` 的包 Source 置 null | 解绑 = 摊平 | 丢失分组/来源信息；与"组是可保留的一等公民"方向冲突。摊平应是用户显式 Ungroup，不是解绑的副作用 |
| 解绑时删除所有整合包包 | 解绑 = 卸载 | 破坏性太大，与现状（保留为手动）不符 |
