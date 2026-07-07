# 包列表分组 UI（蛇形折叠列表）

> 制定日期：2026-06-29
> 定位：UI 层任务，是 Recipe 系统的前置条件，让整合包/recipe/手动三类包在界面上可区分、可整组操作。
> 当前状态：**未实施**。本文档是未来实施的蓝本，据此编码不需重新调研。
> Jira：[POLY-119](https://d3ara1n.atlassian.net/browse/POLY-119)
> 依赖：[POLY-118](SOURCE-REFERENCE-SEMANTICS.md)（✅ 已完成，分组依据语义）、[POLY-116](DEPLOYMENT-PRIORITY-BY-GROUP.md)（✅ 已完成，`SourceOrders` 覆盖顺序 = 组顺序）。
>
> 2026-07-07 校准（第二轮，设计已收敛）：
>
> - POLY-118 保留 `Source` 字段名、未改 `Reference`，产出 `Utilities/PackageSourceHelper.cs`。本任务**全面接入该 Helper**（`Kind`/`Classify`/`CanUpdate`/`CanUngroup`），稳定算法集中维护，不到处散写 `source` 比较。
> - `PackageSourceHelper.Kind.Legacy` **已删**（职责并入 `Modpack`，`Classify` 同步去掉 `current` 参数，编译通过）——UI 不区分"当前/解绑整合包"，都是整合包组。排查确认 deploy pipeline 未消费 `Kind`，仅 UI 侧 3 处用 `CanUpdate`，安全。
> - 组顺序：**组间按 `Profile.Rice.SourceOrders`**（与 POLY-116 共用），**组内按包的 `PersistentIndex`**。
> - **散装包（`Source == null`）不分组**，散开沉底，永远在所有组之后。
> - **单 `ItemsControl` + 拍平序列**保证虚拟化（嵌套容器会破坏虚拟化，成百上千个带 bitmap 的 item 不虚拟化会在筛选时卡死）。
> - **退役 LayoutSelector（List/Grid 切换）**，只做 List 布局。
> - 导引线的 hover/点击定位等交互**本期只做视觉，交互留 TODO**。

---

## 0. 本任务在 Recipe 系统中的位置

> Recipe 系统总纲：[RECIPE-SYSTEM.md](RECIPE-SYSTEM.md)

Recipe 落地后，实例里会同时有整合包、recipe、手动三类包。如果界面仍是扁平列表，用户无法一眼看出"哪些是整合包带的、哪些是 recipe 带的、哪些是我自己加的"——这正是 GitHub #73 的原始痛点。

本任务把扁平包列表改造成**按来源分组的蛇形折叠列表**：每个组是一张头部卡片拖着一条由包卡片组成的"身子"，左侧一条导引线把头和身子连成一体，让归属一目了然，并提供组级批量控制。

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

1. 包列表按来源**蛇形分组**折叠展示：整合包组 / recipe 组各自带头部卡片 + 身子（缩进的包卡片 + 左侧导引线）；**散装包（`Source == null`）不分组，顶格散开沉底**。
2. **现有能力零损失**：文本搜索、启用/禁用/锁定/类型/标签过滤全部保留；过滤只隐藏包，**不隐藏组头**（组空显示 `0/N`）。
3. **组级操作**：整组启用/禁用、整组移除/解散（收进组头「更多」菜单）。
4. **单项操作不变**：组内包与散装包用同一个 `InstancePackageButton`，启停/详情/标签等操作与现状一致。
5. **虚拟化必须保留**：单 `ItemsControl` + `VirtualizingStackPanel`，支撑成百上千个包。

**非目标（本次不做）**

- 不改 `Source` 归属语义（POLY-118 已交付；`PackageSourceHelper` 已修订为三类 `Kind`，`Classify` 去 `current` 参数）。
- 不实现 recipe 导入/管理（Recipe 系统任务）——但本任务的分组能自动接纳 recipe 组。
- 不改部署引擎（POLY-116 任务）——组间顺序读 `SourceOrders`，本任务只读不写（拖拽留后续）。
- **不做组拖拽排序**（本期组间顺序只读 `SourceOrders` 默认值，拖拽写回留后续）。
- **不做导引线交互**（hover 高亮、点击定位回首部）——本期只做导引线视觉，交互标 TODO。
- 不做 Grid 布局（退役 LayoutSelector，只做 List）。
- 不改单个包的渲染控件 `InstancePackageButton`。

---

## 3. 核心设计

### 3.1 分组依据：复用 PackageSourceHelper

**全面接入 `PackageSourceHelper`**，不自建分类。该工具是纯函数，以 `Entry.Source`（部分方法还需实例引用 `profile.Rice.Source`）为输入。

`Kind` 现为三类（`Legacy` 已删，语义并入 `Modpack`——UI 不区分"当前绑定/已解绑整合包"，都当整合包组）。`Classify` 的 `recipe` 判定前置（`recipe://` source 本就不等于当前引用，须先于 `Modpack` 兜底），并去掉了不再需要的 `current` 参数：

```csharp
public enum Kind { Manual, Modpack, Recipe }

public static Kind Classify(string? source) =>
    source is null                              ? Kind.Manual
  : InternalUriHelper.IsKind(source, "recipe")  ? Kind.Recipe
  : Kind.Modpack;
```

> `CanUpdate` / `CanUngroup` / `CanRemove` 保留，组级命令直接复用（§3.3）。它们内部仍用 `source != current` 等原子比较，与 `Kind` 无关——`Kind` 只服务 UI 分组与组头样式。

散装（`Kind == Manual`，即 `Source == null`）**不成组**，走独立的散装段（§3.2）。其余两类（`Modpack` / `Recipe`）成组。

### 3.2 组顺序与散装沉底

- **组间顺序**：按 `Profile.Rice.SourceOrders`（POLY-116 的覆盖顺序，与部署共用同一份数据）。组的 `Source` 在 `SourceOrders` 里的 index 决定其位置；不在列表里的组按默认档位（整合包低于 recipe）。本期**只读默认顺序，不做拖拽写回**。
- **组内顺序**：按包的 `PersistentIndex`（现状即如此，不改）。
- **散装沉底**：散装 entry 共享一个虚拟的 `Loose` 组对象（`PersistentIndex = int.MaxValue`），sorter 自然把它排到所有组之后。散装不产出 Header item（§3.3），只产出 Entry。

默认视觉顺序：

```
整合包组（头+身）→ recipe 组（头+身）→ ... → 散装包（顶格，无头无线）
```

### 3.3 数据模型：共享 GroupModel + 拍平的 PackageListItem

**核心思想——"反着包裹"**：不是"组容器包含 entry"（会破坏虚拟化），而是**每个 list item（无论 Header 还是 Entry）都持有一个共享的 `PackageGroupModel` 引用**，通过这个共享引用表达归属。数据层（DynamicData）维护形状，UI 层只管渲染。

```csharp
// Models/PackageGroupModel.cs —— 共享的"组"对象，header 与所有 entry 引用同一个实例
public sealed partial class PackageGroupModel
{
    public required PackageSourceHelper.Kind Kind { get; init; }   // Manual=散装虚拟组
    public required string? Source { get; init; }                  // 组键 Source；散装=null
    public required string DisplayName { get; init; }              // "整合包: XXX" / "Recipe: QoL" / "手动添加"
    public required int PersistentIndex { get; init; }             // 组级排序键；散装=int.MaxValue

    [ObservableProperty] private bool isExpanded = true;           // 折叠态（散装组恒 true）
    // NOTE: 导引线交互态（hover/点击）本期不建模，留 TODO
}

// Models/PackageListItem.cs —— 拍平的列表项 union，每项拖着同一个 Group 引用
public abstract record PackageListItem
{
    public required PackageGroupModel Group { get; init; }

    public sealed record Header : PackageListItem;                           // 组头项
    public sealed record Entry(InstancePackageModel Package) : PackageListItem;  // 包项（组内/散装）
}
```

**关键性质**：Header 项和它的所有 Entry 项**引用同一个 `PackageGroupModel` 实例**。这是 sorter/filter 不用"比对 source"的前提——直接读 `item.Group`。

**组级命令**挂 `InstanceSetupPageModel`（沿用现状包级命令的宿主惯例），参数传 `PackageGroupModel`：

```csharp
[RelayCommand] private async Task ToggleGroupEnabledAsync(PackageGroupModel? group);  // 整组翻转 Enabled，收进「更多」菜单
[RelayCommand] private async Task RemoveGroupAsync(PackageGroupModel? group);         // 整组移除/解散
```

`RemoveGroupAsync` 的语义由 `PackageSourceHelper.CanUngroup` 决定：

- **Modpack**（`CanUngroup == false`）：不可整组移除，菜单项禁用。须先解绑实例的整合包引用，降级后再处理。
- **Recipe**（`CanUngroup == true`）：解散该组——清空组内包的 `Source`（降为 Manual 散装）或直接删除。
- 散装段无组头，无整组操作；单项删除走现有 `RemovePackageCommand`。

落盘沿用现有 `ProfileManager.TryGetMutable → guard → DisposeAsync` 模式（参照 `RemovePackageAsync` at `InstanceSetupPageModel.cs:1236`）。高危操作（如清空）走 `overlayService.PopDialogAsync` 二次确认（参照 `EditLoaderAsync` at `:565`）。

### 3.4 DynamicData 管道：单 FlatView，sorter 成型，filter 折叠

**一条管道**，产出拍平的 `ReadOnlyObservableCollection<PackageListItem> FlatView`，绑到单个 `ItemsControl`。现状的 `StageView`（扁平包集合）由 `FlatView` 取代。

```
_stageSource.Connect()
    .Filter(enability).Filter(lockility).Filter(kind).Filter(tags).Filter(text)   // 共享过滤谓词（抽成实例复用）
    .Group(GetGroupKey)                          // GetGroupKey: source==null→散装键, 其余→(Kind, source)
    .TransformMany(FlattenGroup)                 // 每组拍平成 PackageListItem 序列（§3.3）
    .AutoRefresh(item => item.Group.IsExpanded)  // 组折叠态变化→重判引用该组的 entry
    .Filter(item => item is Header || item.Group.IsExpanded)                       // 折叠 filter
    .Sort(ItemOrderComparer)                     // sorter 成型（§下方）
    .Bind(out _flatView)
    .Subscribe();
```

`FlattenGroup`：有 `Source` 的组产出 `[Header, Entry, Entry, ...]`；散装组（`Source == null`）只产出 `[Entry, Entry, ...]`（无 Header），共享虚拟 `Loose` GroupModel。

**Sorter 成型**——"sorter 负责形状"。每个 item 自带 `Group` 引用，sort key 不碰 source 字符串：

```csharp
// sort key = (组的 SourceOrders 档位, Header<Entry, 组内 PersistentIndex)
int Compare(PackageListItem a, PackageListItem b)
{
    var c = GroupOrderOf(a.Group).CompareTo(GroupOrderOf(b.Group)); if (c != 0) return c;
    c = RankOf(a).CompareTo(RankOf(b));                        // Header=0, Entry=1 → 头永远在身前
    if (c != 0) return c;
    return IntraIndexOf(a).CompareTo(IntraIndexOf(b));         // 组内按 Package.PersistentIndex
}
// GroupOrderOf: SourceOrders.IndexOf(group.Source)，散装=int.MaxValue；不在列表的组按默认档位
// RankOf: Header→0, Entry→1
// IntraIndexOf: Entry→Package.PersistentIndex, Header→0
```

**Filter 折叠不伤组头**：折叠 filter 对 `Header` 永远放行，只对 `Entry` 判 `Group.IsExpanded`。因同组 entry 共享同一个 GroupModel 对象，`IsExpanded` 一变，`AutoRefresh` 重判该组所有 entry——header 留着、entry 整组进出，无需跨 item 找 header。

**搜索/类型过滤不隐藏组头**：搜索、kind、tag 等过滤作用在 `Entry` 的 `Package` 上（`item is Entry e && PackageMatches(e.Package)`），`Header` 永远放行。组里包全被滤掉，组头仍在（计数变 `0/N`）。

### 3.5 搜索穿透与命中计数

文本搜索作用在包数据层（`Entry.Package`）。命中时：

- 命中包所在组的 `IsExpanded` 自动置 true（订阅搜索文本变化，写入命中包所属 GroupModel）。
- 组头显示命中数（如 `·3/8`），由 `PackageGroupModel` 上的命中计数字段驱动（搜索订阅遍历 FlatView 各组算命中数写入）。组本身无包也是 `0/0`。
- 未命中组可手动折叠减少干扰；组头不因无命中而消失。

一级搜索框天然穿透到折叠内容，无需二级页面。

### 3.6 UI 结构：单 ItemsControl + 三模板 + 蛇形导引线

**单 `ItemsControl` + `VirtualizingStackPanel Spacing="0"`**，绑 `FlatView`。`Spacing="0"` 是导引线连续的充要条件——相邻 Entry 容器上下紧贴，左竖条拼成整线；包卡片的视觉间距改由容器内 `Margin`（如 `0,1.5`）补偿。

item 模板按 `PackageListItem` 子类型三选一（`DataTemplateSelector` 或 `ContentControl` + 多 `DataTemplate`）：

```
┌──────────────────────────────────────────────────────┐
│ ╔═ Header 模板 ══════════════════════════════════╗   │  独立组头卡片（蛇头）
│ ║ ▼ 📦 整合包·ATM9            ·8/15          ⋯ ║   │  点击折叠；右侧「更多」菜单
│ ╚═══════════════════════════════════════════════╝   │
│ ┊┌──────────────────────────────────────────────┐   │  Entry(grouped) 模板
│ ┊│ [□] Imported Mod ATM Sapphire      @team ◯ │   │  左竖条(导引线)+缩进+InstancePackageButton
│ ┊└──────────────────────────────────────────────┘   │  上下 Margin 0,1.5 补间距，竖条紧贴拼接
│ ┊┌──────────────────────────────────────────────┐   │  Entry(grouped)
│ ┊│ [□] Imported Mod ATM Iron          @team ◯ │   │
│ ┊└──────────────────────────────────────────────┘   │
│ ╔═ Header(Recipe) ══════════════════════════════╗   │
│ ║ ▶ 📖 Recipe·QoL              ·0/0          ⋯ ║   │  空组也显示 0/0
│ ╚═══════════════════════════════════════════════╝   │
│ ┌──────────────────────────────────────────────┐   │  Entry(loose) 模板
│ │ [□] Mod 手动包A                     @xxx  ◯ │   │  顶格，无竖条，InstancePackageButton
│ └──────────────────────────────────────────────┘   │  散装沉底
└──────────────────────────────────────────────────────┘
```

骨架（示意）：

```xml
<ScrollViewer>
  <ItemsControl ItemsSource="{Binding FlatView}">
    <ItemsControl.ItemsPanel>
      <VirtualizingStackPanel Spacing="0"/>          <!-- 间距0，保证导引线连续 -->
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
      <DataTemplate DataType="PackageListItem">       <!-- 顶层 ContentControl 按子类型选模板 -->
        <ContentControl Content="{Binding}">
          <ContentControl.DataTemplates>
            <!-- Header：独立组头卡片，点击折叠，右侧「更多」菜单 -->
            <DataTemplate DataType="PackageListItem+Header">
              <app:PackageGroupHeader .../>
            </DataTemplate>
            <!-- Entry：左导引线列 + 缩进 + 同一个 InstancePackageButton -->
            <DataTemplate DataType="PackageListItem+Entry">
              <Grid Margin="0,1.5" ColumnDefinitions="24,*">
                <Border Grid.Column="0" Width="2" HorizontalAlignment="Center"
                        Background="{Binding 组来源色}"
                        IsVisible="{Binding ! (Group.Kind == Manual)}"/>   <!-- 散装无线 -->
                <app:InstancePackageButton Grid.Column="1" .../>            <!-- 同一个控件 -->
              </Grid>
            </DataTemplate>
          </ContentControl.DataTemplates>
        </ContentControl>
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</ScrollViewer>
```

**导引线**＝相邻 grouped Entry 的左 Border 段拼接（`Spacing="0"` + 容器 `Margin="0,1.5"`），从组头正下方延伸到组末包。颜色按组来源（整合包/recipe 各一色）作为归属签名。**导引线的 hover 高亮、点击定位回首部等交互本期不做**，标 TODO——视觉先行，交互留待后续基于共享 GroupModel 设计。

**LayoutSelector 退役**：移除 `InstanceSetupPage.axaml:662` 的 TabStrip、`ViewState.LayoutIndex`、3 个 `#LayoutSelector...` 绑定；两套内联模板只保留 List（`VirtualizingStackPanel`），ItemTemplate 直接内联进 Entry 模板。`ListTemplateCombinationModel` 类不删（`MarketplaceSearchPage.axaml` 仍在用）。

样式遵循 Huskui 规范：组头是变体类（PascalCase class），展开/折叠是状态伪类（`:expanded`，见项目 AGENTS.md 样式约定）。

---

## 4. 改动面

| 层 | 文件 | 改动 |
|----|------|------|
| 模型 | `Models/PackageGroupModel.cs`（新增） | 共享组对象：`Kind`/`Source`/`DisplayName`/`PersistentIndex`/`IsExpanded`（无命令，命令挂 PageModel） |
| 模型 | `Models/PackageListItem.cs`（新增） | 拍平 union：`Header` / `Entry(Package)`，都带 `Group` 引用 |
| 模型 | `Controls/PackageGroupHeader.axaml(.cs)`（新增） | 组头卡片控件：折叠触发区 + 标题 + 计数 + 「更多」菜单 |
| 管道 | `PageModels/InstanceSetupPageModel.cs` | `StageView` → `FlatView`；`.Group().TransformMany(FlattenGroup).AutoRefresh.Filter.Sort`；`GetGroupKey`（薄封装 `Classify`）；组级命令 `ToggleGroupEnabled`/`RemoveGroup`；命中计数订阅 |
| 视图 | `Pages/InstanceSetupPage.axaml` | 扁平 `ItemsControl` → 单 `ItemsControl` + 三模板 + `VirtualizingStackPanel Spacing=0`；退役 LayoutSelector |
| 样式 | `Themes/` 或 `Controls/` | 组头 ControlTheme（变体类 + `:expanded` 伪类）；导引线来源色 |
| 持久化 | ViewState（展开态） | 组展开/折叠态走 ViewState（轻量）；组间顺序只读 `SourceOrders`，本期不写回 |

---

## 5. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 整合包实例 | 整合包包归一组（头+身+导引线），手动包散开沉底顶格 |
| 2 | 折叠/展开组 | 点组头切换 `IsExpanded`，组内 entry 整组进出（数据层，非 IsVisible） |
| 3 | 整组启用/禁用 | 组头「更多」菜单翻转组内所有包 Enabled |
| 4 | 整组移除/解散 | Modpack 组不可整组移除（菜单禁用）；Recipe 组可解散（清 Source）；散装无整组操作 |
| 5 | 文本搜索命中 | 命中组自动展开，组头显示 `命中/总数`；未命中组头不消失 |
| 6 | 过滤（仅显示禁用） | 各组仅显示禁用项；组空时组头仍在，显示 `0/N` |
| 7 | recipe 包（未来） | 自动归入 recipe 组，组名取 recipe 名 |
| 8 | 组顺序 | 整合包→recipe 默认顺序（读 `SourceOrders`）；散装永远沉底 |
| 9 | 单项操作 | 组内包与散装包用同一个 `InstancePackageButton`，启停/详情/编辑/删除与现状一致 |
| 10 | 虚拟化 | 成百上千包时筛选不卡（单 `ItemsControl` + `VirtualizingStackPanel` 生效） |
| 11 | 导引线视觉 | 相邻组内包左竖条拼接成连续长线，从组头延伸到组末包；散装无导引线 |

---

## 6. 风险与取舍

| 风险 | 取舍 |
|------|------|
| 嵌套容器破坏虚拟化 | 选**单 ItemsControl + 拍平序列**（§3.3/§3.4），放弃"组容器包裹 entry"的自然写法；代价是数据层要拍平成型 |
| DynamicData 拍平管道的响应式正确性 | 包增删/过滤/折叠/排序变化都要正确反映到 FlatView 对应位置；`.Group().TransformMany().AutoRefresh` 能处理，但折叠重算（`IsExpanded` 变）要测——这是唯一真实工程难点，非阻断 |
| 组头与单项控件复用 `InstancePackageButton` 的样式冲突 | 组头独立模板（`PackageGroupHeader`），不复用包按钮；Entry 模板内联同一个 `InstancePackageButton` |
| 导引线段拼接断点 | 面板 `Spacing="0"` + 容器 `Margin="0,1.5"` 补间距，竖条紧贴拼接；间距感由容器内 margin 给 |
| 导引线交互复杂度 | 本期只做视觉，hover/点击定位留 TODO（基于共享 GroupModel 设计，成本可控） |
| LayoutSelector 退役影响已有 Grid 视图用户 | 本次明确只做 List 布局，Grid 兼容写法留待未来（§3.6） |

---

## 7. 不做的事（明确边界）

- **不改 Source 归属语义** —— 全面接入 POLY-118 的 `PackageSourceHelper`（`Kind` 已为三类，`Legacy` 并入 `Modpack`）。
- **不区分 Legacy 整合包** —— UI 眼里只有整合包组（Modpack，含当前/解绑）与 recipe 组，解绑整合包不单独标识。
- **不做组拖拽排序** —— 组间顺序本期只读 `SourceOrders` 默认值，拖拽写回留后续。
- **不做导引线交互** —— 本期只做导引线视觉，hover 高亮/点击定位留 TODO。
- **不实现 recipe** —— recipe 组随 Recipe 系统自然出现。
- **不改部署引擎** —— 组顺序作为优先级输入交给 POLY-116，本任务只读 `SourceOrders`。
- **不做二级页面/Modal 形态** —— 已选折叠结构（§1.3）。
- **不做 Grid 布局的分组** —— 退役 LayoutSelector，只做 List（§3.6）。
- **不改 `InstancePackageButton` 内部** —— 单项渲染不变，组内/散装共用。

---

## 附录：备选方案备案

### D.1 分组形态

当前选：**蛇形折叠列表**（§1.3 + §3.6）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 二级页面（点组进子页） | 组作为导航项 | 搜索/过滤无法穿透；归属割裂；已否决 |
| 纯扁平 + 来源筛选器 | 不分组，加"按来源筛选"下拉 | 不满足 #73 的"分组呈现"诉求；归属仍不可一眼看出 |
| TreeView | 用 Avalonia `TreeView` 原生 | TreeView 语义是层级数据，包列表本质是单层分组；自定义 ItemsControl 更贴合样式控制需求 |

### D.2 分组容器的底层实现

当前选：**单 ItemsControl + 拍平 PackageListItem 序列**（§3.3/§3.4）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 嵌套 ItemsControl（外层组、内层包） | 每组一个 StackPanel 包 header + 子 ItemsControl | 破坏虚拟化——外层面板无法统一回收子 ItemsControl 的项；成百上千个带 bitmap 的 item 不虚拟化会在筛选时卡死 |
| Avalonia `Expander` 包组内容 | 复用原生折叠 | Header 塞复杂交互（计数/菜单/导引线）会与折叠点击冲突；且仍属嵌套容器，虚拟化问题不变 |

拍平方案的代价是数据层要成型（sorter 保证 header 在 entry 前、filter 折叠 entry 不伤 header），换来虚拟化与完全的视觉可控。

### D.3 组顺序持久化位置

当前选：**组间顺序复用 `Profile.Rice.SourceOrders`（与 POLY-116 部署共用），展开态存 ViewState**

| 备选 | 做法 | 取舍 |
|------|------|------|
| 新建专用组顺序字段 | `Profile.Rice` 加 `GroupOrders` | 与 `SourceOrders` 语义重叠（POLY-116 已定覆盖顺序 = 组顺序），双源易不一致 |
| 全存 ViewState | 组顺序也存 ViewState | 组顺序影响部署优先级，是实例语义，应随实例走；放 ViewState 跨实例串扰 |
| 全存 Profile（含展开态） | 展开态也进 profile.json | 展开是 UI 偏好，非实例属性，污染 Profile；ViewState 更合适 |

### D.4 组级"移除"的语义

当前选：**按 `CanUngroup` 差异化**（§3.3）

| 备选 | 做法 | 取舍 |
|------|------|------|
| 统一"删除组内所有包" | 不区分来源，纯删包 | 整合包组删除后 `profile.Rice.Source` 残留、包级 `Source` 残留，语义不干净；需差异化处理来源字段 |
| 只允许隐藏不允许删 | 组可折叠不可删 | 不满足"移除 recipe/卸载整合包"诉求 |
