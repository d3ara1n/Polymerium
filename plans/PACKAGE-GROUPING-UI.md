# 包列表分组 UI（折叠结构）

> 制定日期：2026-06-29
> 定位：UI 层任务，是 Recipe 系统的前置条件，让整合包/recipe/手动三类包在界面上可区分、可整组操作。
> 当前状态：**未实施**。本文档是未来实施的蓝本，据此编码不需重新调研。
> Jira：[POLY-119](https://d3ara1n.atlassian.net/browse/POLY-119)
> 依赖：[POLY-118](SOURCE-REFERENCE-SEMANTICS.md)（分组依据语义）。

---

## 0. 本任务在 Recipe 系统中的位置

> Recipe 系统总纲：[RECIPE-SYSTEM.md](RECIPE-SYSTEM.md)

Recipe 落地后，实例里会同时有整合包、recipe、手动三类包。如果界面仍是扁平列表，用户无法一眼看出"哪些是整合包带的、哪些是 recipe 带的、哪些是我自己加的"——这正是 GitHub #73 的原始痛点。

本任务把扁平包列表改造成**按来源分组的折叠列表**，让归属一目了然，并提供组级批量控制（整体启停、整组移除、组排序）。先做到让整合包分组可用，recipe 分组随 Recipe 系统自然生效（分组依据是 `Source`，recipe 包的 `Source = recipe://` 会自动成组）。

---

## 1. 背景与动机

### 1.1 现状：扁平列表

包管理主页面 `InstanceSetupPage`（`Pages/InstanceSetupPage.axaml` + `PageModels/InstanceSetupPageModel.cs`）用 DynamicData 管道把 `SourceCache<InstancePackageModel, Entry>` 经多重过滤排序后绑定为扁平 `ReadOnlyObservableCollection<InstancePackageModel> StageView`：

```
_stageSource.Connect()
    .Filter(FilterEnability)
    .Filter(FilterLockility)
    .Filter(FilterKind)
    .Filter(FilterText)
    .SortBy(x => x.PersistentIndex)
    .Bind(out _stageView)
```

渲染是单个 `ItemsControl`（`InstanceSetupPage.axaml:749-754`），每个项是 `InstancePackageButton`。**没有分组、没有折叠**，所有包按 `PersistentIndex` 线性铺开。

### 1.2 问题

- **归属不可见**：整合包带来的包和手动加的包混在一起，UI 上无任何区分（只有锁定图标隐含，但用户难解读）。
- **无组级操作**：想"禁用整个整合包的所有包"只能逐个点；想"移除整个 recipe"只能一个个删。
- **#73 的直接诉求未满足**：用户明确要求"按所属整合包分组呈现""支持整体禁用/启用""展开分组单独控制"。

### 1.3 为什么选折叠结构而非二级页面

前期讨论评估过两种形态：

| 形态 | 优点 | 缺点 |
|------|------|------|
| **二级页面**（点组进子页看组内包） | 组内空间大 | 一级页面的搜索/过滤无法穿透到二级内容；用户要进组才能看包，归属感反而割裂 |
| **折叠结构 grouped list** | 原搜索/过滤能力零损失（作用于底层数据）；组头 + 展开内容同屏；组级操作和单项操作并存 | 单屏信息密度高，需控制组头紧凑 |

选**折叠结构**。它保留了现有 DynamicData 管道的全部过滤/排序能力（这些作用在扁平数据上），分组只是渲染层在过滤结果上的聚合，搜索命中时自动展开目标组。

---

## 2. 目标与非目标

**目标**

1. 包列表按 `Source` 分组折叠展示：手动组（`Source == null`）/ 整合包组（`Source == Reference`）/ recipe 组（`Source` 是 `recipe://`）。
2. **现有能力零损失**：文本搜索、启用/禁用/锁定/类型/标签过滤、排序全部保留。
3. **组级操作**：整组启用/禁用、整组移除、组排序（影响部署优先级，见 DEPLOYMENT-PRIORITY-BY-GROUP）。
4. **单项操作不变**：展开组后，包的启停/详情/标签等操作与现状一致。
5. 搜索命中时，目标组自动展开并定位。

**非目标（本次不做）**

- 不改 `Source` / `Reference` 语义（SOURCE-REFERENCE-SEMANTICS 任务）。
- 不实现 recipe 导入/管理（Recipe 系统任务）——但本任务的分组能自动接纳 recipe 组。
- 不改部署引擎（DEPLOYMENT-PRIORITY-BY-GROUP 任务）——组排序的结果作为部署优先级输入。
- 不改单个包的渲染控件 `InstancePackageButton`（仅可能加组内缩进容器）。

---

## 3. 核心设计

### 3.1 分组依据与组键

DynamicData 管道在过滤排序后追加 `.GroupBy(GetGroupKey)`，组键定义：

```csharp
PackageGroupKey GetGroupKey(InstancePackageModel m)
{
    var source = m.Entry.Source;
    if (source is null)
        return new(PackageGroupKind.Manual, null);
    if (source == Reference)                       // 依赖 SOURCE-REFERENCE-SEMANTICS
        return new(PackageGroupKind.Modpack, source);
    if (InternalUri.IsKind(source, "recipe"))      // 依赖 URL-SCHEME-UNIFICATION
        return new(PackageGroupKind.Recipe, source);
    return new(PackageGroupKind.External, source); // 防御性，理论上不出现
}

public enum PackageGroupKind { Modpack, Recipe, Manual, External }
```

组键同时携带 `source` 字符串，用于组头显示来源名（整合包名/recipe 名，需查元数据解析）。

### 3.2 组排序（= 部署优先级的前序）

组的显示顺序需稳定且有意义。**组顺序直接对应部署优先级**（见 DEPLOYMENT-PRIORITY-BY-GROUP）：手动组优先级最高（最后部署、覆盖力最强），整合包最低。默认组顺序：

```
整合包组 → recipe 组 → 手动组
（优先级低）            （优先级高）
```

用户可拖拽调整 recipe 组之间的相对顺序（多个 recipe 时）。整合包和手动组位置固定（只有一个整合包、一个手动组）。组顺序需持久化（recipe 组顺序），存 `Profile.Rice` 新增字段或 ViewState。

### 3.3 分组视图模型

新增 `Models/PackageGroupModel.cs`（嵌套于合适的 ViewModel 或独立）：

```csharp
public class PackageGroupModel
{
    public required PackageGroupKind Kind { get; init; }
    public required string? Source { get; init; }       // 组键 source
    public required string DisplayName { get; init; }   // "整合包: All The Mods" / "Recipe: QoL" / "手动添加"
    public required bool IsExpanded { get; set; }       // 折叠态
    public ReadOnlyObservableCollection<InstancePackageModel>? Items { get; set; }

    // 组级命令（RelayCommand）
    [RelayCommand] Task ToggleAllEnabledAsync();   // 整组翻转 Enabled
    [RelayCommand] Task RemoveGroupAsync();        // 整组移除（整合包=卸载来源, recipe=取消链接, 手动=清空）
    [RelayCommand] Task ExpandAsync();
}
```

`RemoveGroupAsync` 的语义因组类型不同：整合包组移除 = 解除 `Reference` + 删该组包；recipe 组移除 = 取消该 recipe 链接 + 删该组包；手动组移除 = 清空手动包（高危，需确认）。

### 3.4 DynamicData 管道改造

现有管道在 `.Bind(out _stageView)` 之前插入分组：

```
_stageSource.Connect()
    .Filter(FilterEnability)
    .Filter(FilterLockility)
    .Filter(FilterKind)
    .Filter(FilterText)
    .SortBy(x => x.PersistentIndex)
    .Group(GetGroupKey)                       // 新增：产出 IObservable<IReadOnlyList<PackageGroupModel>>
    .Sort(...)                                 // 组排序（§3.2）
    .Bind(out _groupedView)
```

`StageView`（扁平）保留用于搜索命中展开逻辑，`GroupedView` 绑定到 UI。过滤仍作用在扁平数据层——某个包被过滤掉，它所在的组自动少一项；组空了组头隐藏。

### 3.5 搜索穿透

文本搜索（`FilterText`）作用在扁平包数据上。命中时：

- 被命中包所在的组自动 `IsExpanded = true`。
- 组头显示命中数（如 "Recipe: QoL (3/12 命中)"）。
- 未命中的组可折叠以减少干扰。

这样一级搜索框天然穿透到折叠内容，无需二级页面。

### 3.6 UI 结构

`InstanceSetupPage.axaml` 的 `ItemsControl` 替换为分组容器：

```xml
<ItemsControl ItemsSource="{Binding GroupedView}">
    <ItemsControl.ItemTemplate>
        <DataTemplate DataType="PackageGroupModel">
            <Border>
                <!-- 组头：名称 + 命中数 + 整组开关 + 展开箭头 + 移除按钮 -->
                <Grid>...</Grid>
                <!-- 组内容：展开时显示 -->
                <ItemsControl IsVisible="{Binding IsExpanded}"
                              ItemsSource="{Binding Items}"
                              ItemTemplate="{Binding #LayoutSelector...}" />
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

样式遵循 Huskui 规范：组头是变体类（PascalCase class），展开/折叠是状态伪类（`:expanded`，见项目 AGENTS.md 样式约定）。

---

## 4. 改动面

| 层 | 文件 | 改动 |
|----|------|------|
| ViewModel | `Models/PackageGroupModel.cs`（新增） | 分组视图模型 + 组级命令 |
| 管道 | `PageModels/InstanceSetupPageModel.cs` | DynamicData 追加 `.Group`；`GroupedView` 绑定；`GetGroupKey`；搜索命中展开逻辑 |
| 视图 | `Pages/InstanceSetupPage.axaml` | 扁平 `ItemsControl` → 分组容器；组头模板 |
| 样式 | `Themes/` 或 `Controls/` | 组头 ControlTheme（变体类 + `:expanded` 伪类） |
| 持久化 | `Profile.Rice`（新增组顺序字段）或 `ViewState` | recipe 组顺序持久化 |

---

## 5. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 整合包实例 | 整合包包归一组"整合包: XXX"，手动包归"手动添加" |
| 2 | 折叠/展开组 | 点组头箭头切换，内容显隐 |
| 3 | 整组启用/禁用 | 组头开关翻转组内所有包 Enabled |
| 4 | 整组移除 | 整合包组移除 = 解除来源；手动组移除需二次确认 |
| 5 | 文本搜索命中 | 目标组自动展开，组头显示命中数 |
| 6 | 过滤（仅显示禁用） | 各组仅显示禁用项；空组隐藏 |
| 7 | recipe 包（未来） | 自动归入 recipe 组，组名取 recipe 名 |
| 8 | 组顺序 | 整合包→recipe→手动默认顺序；recipe 组间可拖拽排序 |
| 9 | 单项操作 | 展开组后，包的启停/详情与现状一致 |

---

## 6. 风险与取舍

| 风险 | 取舍 |
|------|------|
| 折叠态持久化增加状态 | 组展开/折叠态存 ViewState（轻量）；recipe 组顺序存 Profile（影响部署） |
| 组头与单项控件复用 `InstancePackageButton` 的样式冲突 | 组头独立模板，不复用包按钮；仅缩进容器共享样式 |
| DynamicData `.Group` 后排序/过滤链路变复杂 | DynamicData 原生支持分组，管道仍声明式；可读性可控 |
| 组内包很多时展开渲染压力 | 项数大时可加虚拟化（`VirtualizingStackPanel`）；当前规模无压力 |

---

## 7. 不做的事（明确边界）

- **不改 Source/Reference 语义** —— 分组依据来自 SOURCE-REFERENCE-SEMANTICS。
- **不实现 recipe** —— recipe 组随 Recipe 系统自然出现。
- **不改部署引擎** —— 组顺序作为优先级输入交给 DEPLOYMENT-PRIORITY-BY-GROUP。
- **不做二级页面/Modal 形态** —— 已选折叠结构（§1.3）。
- **不改 `InstancePackageButton` 内部** —— 单项渲染不变。

---

## 附录：备选方案备案

### D.1 分组形态

当前选：**折叠结构 grouped list**（§1.3）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 二级页面（点组进子页） | 组作为导航项 | 搜索/过滤无法穿透；归属割裂；已否决 |
| 纯扁平 + 来源筛选器 | 不分组，加"按来源筛选"下拉 | 不满足 #73 的"分组呈现"诉求；归属仍不可一眼看出 |
| TreeView | 用 Avalonia `TreeView` 原生 | TreeView 语义是层级数据，包列表本质是单层分组；自定义 ItemsControl 更贴合样式控制需求 |

### D.2 组级"移除"的语义

当前选：**按组类型差异化**（§3.3）

| 备选 | 做法 | 取舍 |
|------|------|------|
| 统一"删除组内所有包" | 不区分来源，纯删包 | 整合包组删除后 `Reference` 残留、`Source` 残留，语义不干净；需差异化处理来源字段 |
| 只允许隐藏不允许删 | 组可折叠不可删 | 不满足"移除 recipe/卸载整合包"诉求 |

### D.3 组顺序持久化位置

当前选：**recipe 组顺序存 Profile，展开态存 ViewState**

| 备选 | 做法 | 取舍 |
|------|------|------|
| 全存 Profile | 展开态也进 profile.json | 展开是 UI 偏好，非实例属性，污染 Profile；ViewState 更合适 |
| 全存 ViewState | 组顺序也存 ViewState | 组顺序影响部署优先级，是实例语义，应随实例走；放 ViewState 跨实例串扰 |
