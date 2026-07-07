# 包列表分组 UI（蛇形折叠列表）

> 制定日期：2026-06-29
> 定位：UI 层任务，是 Recipe 系统的前置条件，让整合包/recipe/手动三类包在界面上可区分、可整组操作。
> 当前状态：**已实施**（2026-07-07）。下方章节为设计提案；实际实施以 2026-07-07 第四轮校准为准。
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
>
> 2026-07-07 第三轮校准（语义订正 + 管道重做）：
>
> - **组＝外部引用，不是分类。** 组里的包是外部（整合包/recipe）带进来的，成员由外部引用决定，**不能从包列表增删**。`CanRemove` 在组里恒 false（`source != null`），`CanUpdate` 仅当 `source == Setup.Source`（当前绑定整合包）时 false。能增删的只有散装（`source == null`）。
> - **GroupModel 改为多态**：抽象基类 `PackageGroupModel`，现做 `ModpackGroupModel` 与 `LooseGroupModel` 两种；`RecipeGroupModel` 等 Recipe 系统落地再设计，本期不写。`PackageSourceHelper.Kind` 仅作为决定实例化哪种 GroupModel 的派发键，不再做"当前/遗留"区分。
> - **组头内容来自解析 source purl**：source 本身是 purl，解析结果即整合包元数据（名字+图标），复用 `InstanceSetupPage` 顶部 Reference 那套解析；点组头跳转整合包详情（同 Reference）。不再字符串拆 source。
> - **管道重做为双流**（§3.4）：原 `.Group().TransformMany(FlattenGroup)` 在组成员变化时静默丢项——散装增删会丢；改 entries/headers 两条独立动态流用无键 `Or` 合并。
> - **搜索是纯过滤**：不自动展开组、不写计数；组头不显示任何计数。
> - **展开态不持久化**（不入 ViewState）。
>
> 2026-07-07 第四轮校准（实施落地，订正早期提案）：
>
> - **数据模型全用 `ModelBase` 子类，无 record、无 LazyObject**：`PackageListItemBase : ModelBase`（多态，嵌套 `Header` / `Entry(Package)`），两者都持有 `Group : GroupModelBase`——**Group 既是分组依据（同组共享同一实例），又是组信息载体**。`GroupModelBase : ModelBase`（`Kind`/`Source`/`IsExpanded`/`RequireGuideLine`）派生 `ModpackGroupModel`（`[ObservableProperty] Name`/`Thumbnail`/`IsInfoLoaded`）与 `LooseGroupModel`（散装单例，`RequireGuideLine=false`）。
> - **组信息在 Merge-Load 阶段加载**：`TriggerPackageMerge` 算完包 diff 后并行起 `RefreshGroupsAsync`，对每个 Modpack source 走 `PackageHelper.TryParse` + `DataService.ResolvePackageAsync(Kind=Modpack)`，结果写进 `ModpackGroupModel.Name`/`Thumbnail`（`IsInfoLoaded` 守卫防重）。与包信息加载（`RefreshPackagesAsync`）同路、同时机，**不塞 `LazyObject`**。
> - **管道双流 + `AutoRefreshOnObservable`，无 Subject**：`entries = filtered.Transform(pkg=>Entry).RemoveKey()`、`headers = filtered.RemoveKey().Filter(source!=null).GroupOn(key).Transform(group=>Header)`，无键 `Or` 合并；折叠用 `.AutoRefreshOnObservable(item => item.Group.WhenPropertyChanged(g => g.IsExpanded)).Filter(item => item is Header || item.Group.IsExpanded)`——直接订阅共享 GroupModel 的 INPC，点组头翻转 `IsExpanded` 即重判，无需手动 signal。`PackageListItemBase` 是 `ModelBase`（INPC），不再是 record。
> - **Header 渲染用 `ContentControl`**：Group 内容抽成独立资源 `ModpackGroupContentTemplate`（`x:DataType=ModpackGroupModel`），Header 模板里 `<ContentControl Content="{Binding Group}" ContentTemplate="{StaticResource ...}"/>`，不内联 `LazyContainer`。
> - **退役 LayoutSelector**（只留 List）；搜索计数器分子改绑 `FilteredPackageCount`（订阅 `filtered.QueryWhenChanged` 的包数，**不含 Header、不受折叠影响**）。
> - 下方 §3.3/§3.4/§3.5/§3.6 的代码示例为早期提案（record/Subject/LazyContainer），已被本节订正取代；实际代码见 `src/Polymerium.Avalonia`。

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

1. 按外部来源**蛇形分组**折叠展示：每个外部引用（整合包，以及未来的 recipe）是一张头部卡片拖着由包卡片组成的"身子"，左侧导引线连成一体；**散装（`Source == null`）不成组、无头无线、沉底**。
2. **现有能力零损失**：文本搜索、启用/禁用/锁定/类型/标签过滤全部保留；过滤作用于包，组里包全被滤掉时该组头也随之不显示（不保留空组头）。
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
- 不区分主/次 Modpack、当前/遗留整合包 —— Modpack 就是 Modpack，能否更新由 `CanUpdate` 控制、与 Kind 无关。
- 不预先设计 RecipeGroupModel —— recipe 组随 Recipe 系统自然出现，届时再设计。
- 组头不显示计数（命中数/总数）。
- 展开态不入 ViewState（纯临时）。

---

## 3. 核心设计

### 3.1 分组依据：PackageSourceHelper.Kind 派发 GroupModel 子类

`PackageSourceHelper.Classify(string? source)` 返回三类 `Kind`，在本任务里**只作为决定实例化哪种 GroupModel 的派发键**，不做"当前/遗留"区分：

```csharp
public enum Kind { Manual, Modpack, Recipe }

public static Kind Classify(string? source) =>
    source is null                              ? Kind.Manual
  : InternalUriHelper.IsKind(source, "recipe")  ? Kind.Recipe
  : Kind.Modpack;
```

- `Modpack` → `ModpackGroupModel`（本期做）
- `Recipe` → `RecipeGroupModel`（**未来**，Recipe 系统落地再做，本期不设计）
- `Manual`（`source == null`，散装）→ `LooseGroupModel`（共享单例，无头无线）

每条**不同的 `source` 字符串**是一个独立组（同 Kind 下按 source 去重）。组＝外部引用：成员由外部来源决定，包列表不能向组里增删包（`CanRemove` 在组里恒 false）。单包能否改版本仍由 `CanUpdate` 现场判（`source == Setup.Source` 时 false），与组/Kind 无关。组级"解散"由 `CanUngroup` 决定（见 §3.3）。

### 3.2 组顺序与散装沉底

- **组间顺序**：按 `Profile.Rice.SourceOrders`（POLY-116 的覆盖顺序，与部署共用同一份数据）。组的 `Source` 在 `SourceOrders` 里的 index 决定其位置；不在列表里的组按默认档位（整合包低于 recipe）。本期**只读默认顺序，不做拖拽写回**。
- **组内顺序**：按包的 `PersistentIndex`（现状即如此，不改）。
- **散装沉底**：散装 entry 共享一个虚拟的 `Loose` 组对象（`PersistentIndex = int.MaxValue`），sorter 自然把它排到所有组之后。散装不产出 Header item（§3.3），只产出 Entry。

默认视觉顺序：

```
整合包组（头+身）→ recipe 组（头+身）→ ... → 散装包（顶格，无头无线）
```

### 3.3 数据模型：多态 GroupModel + 拍平的 PackageListItem

**核心思想——"反着包裹"**：不是"组容器包含 entry"（会破坏虚拟化），而是**每个拍平后的 list item（无论 Header 还是 Entry）都持有一个共享的 GroupModel 引用**，通过这个共享引用表达归属。数据层（DynamicData）维护形状，UI 层只管渲染。

#### GroupModel（多态，INPC）

基类持有折叠态与组键；子类按来源携带各自的头部数据：

```csharp
// Models/PackageGroupModel.cs —— 抽象基类
public abstract partial class PackageGroupModel : ModelBase
{
    public required PackageSourceHelper.Kind Kind { get; init; }
    public required string? Source { get; init; }        // 组键 source；散装=null
    [ObservableProperty] public partial bool IsExpanded { get; set; } = true;
}

// Models/ModpackGroupModel.cs —— 整合包组
//   source 是 purl，解析得整合包元数据（名字+图标），复用 InstanceSetupPage 顶部
//   Reference 那套解析；头部点一下跳整合包详情（同 Reference 行为）
public sealed partial class ModpackGroupModel : PackageGroupModel
{
    // 解析结果（名字/图标/可点击跳转的目标），具体字段随解析机制定，
    // 本期接入时参照 InstanceSetupPage.Reference 的解析路径落字段
}

// Models/LooseGroupModel.cs —— 散装虚拟组（共享单例，无头无线）
public sealed partial class LooseGroupModel : PackageGroupModel
{
    // Kind=Manual, Source=null, IsExpanded 恒 true；不渲染头与导引线
}
```

GroupModel 实例由页面级**稳定字典** `_groupModels` 持有（按 `(Kind, source)` 去重 `GetOrAdd`），整个页面生命周期存活——切实例清 `_stageSource` 也不重建，`IsExpanded` 不丢，purl 解析不重跑。散装共享一个 `LooseGroupModel` 单例。

> `RecipeGroupModel` 是 Recipe 系统落地后的产物，**本期不设计**。届时新增子类 + 对应头部模板即可，基类与拍平管道无需改。

#### PackageListItem（拍平 union）

```csharp
// Models/PackageListItem.cs —— Header 与所有 Entry 引用同一个 GroupModel 实例
public abstract record PackageListItem
{
    public required PackageGroupModel Group { get; init; }
    public sealed record Header : PackageListItem;
    public sealed record Entry(InstancePackageModel Package) : PackageListItem;
}
```

Header 项与它的所有 Entry 项**引用同一个 GroupModel 实例**——这是 sorter/filter 不比对 source 字符串、直接读 `item.Group` 的前提。

#### 组级命令（挂 `InstanceSetupPageModel`）

```csharp
[RelayCommand] private async Task ToggleGroupEnabledAsync(PackageGroupModel? group);  // 整组翻转 Enabled，收进「更多」菜单
[RelayCommand] private async Task RemoveGroupAsync(PackageGroupModel? group);         // 整组解散（CanUngroup 驱动）
```

- **组＝外部引用，不能向组里增删包**；`ToggleGroupEnabled` 只是批量翻转组内包的 `IsEnabled`（单包级状态聚合，不动成员/引用）。
- `RemoveGroupAsync` 由 `PackageSourceHelper.CanUngroup` 决定：当前整合包组（`source == Setup.Source`）不可解散，菜单项禁用——要管理它去整合包详情（点组头跳转）；其余非空 source 组可解散（清空组内包的 `Source` 降为散装，或直接删除）。散装无组头、无整组操作，单项删除走现有 `RemovePackageCommand`（`CanRemove=true`）。

落盘沿用现有 `ProfileManager.TryGetMutable → guard → DisposeAsync` 模式（参照 `RemovePackageAsync` at `InstanceSetupPageModel.cs:1236`）。高危操作走 `overlayService.PopDialogAsync` 二次确认（参照 `EditLoaderAsync` at `:565`）。

### 3.4 DynamicData 管道：entries/headers 双流 + 无键 Or 合并

一条管道产出拍平的 `ReadOnlyObservableCollection<PackageListItem> FlatView`，绑到单个 `ItemsControl`。现状的 `StageView`（扁平包集合）由 `FlatView` 取代。

**为什么不是 `.Group().TransformMany(FlattenGroup)`**：DynamicData 的 `Group` 算子在组成员增删时**不在外层流发事件**（只在组首次出现/末项消失时发）。若 `FlattenGroup` 返回一次性列表 `[Header, Entry, ...]`，向已存在的组里增删包不会进 FlatView。组本身（外部引用）成员不变，但**散装会增删**（`CanRemove=true`），散装也走这条拍平管道——静态拍平会静默丢散装的增删。所以拆成两条各自动态的流：

```csharp
private readonly Subject<Unit> _expandChanged = new();

var filtered = _stageSource.Connect()
    .Filter(enability).Filter(lockility).Filter(kind).Filter(tags).Filter(text);

// 1) 包流：过滤后的包直接 Transform 成 Entry，天然跟随增删/更新；包归哪个组看它的 Source 现场查 _groupModels
var entries = filtered
    .Transform(pkg => (PackageListItem)new Entry(pkg) { Group = GroupModelOf(pkg) })
    .RemoveKey();                                  // 去 key，为无键 Or 合并

// 2) 头流：按组键去重，每组一个 Header；组生则头生、组灭则头灭。散装无头
var headers = filtered
    .Group(GetGroupKey)                            // IChangeSet<IGroup<...>, GroupKey>，组级增删它管
    .Filter(g => g.GroupKey.Source is not null)    // 散装无组头
    .Transform(g => (PackageListItem)new Header { Group = GroupModelOf(g.GroupKey) })
    .RemoveKey();

// 3) 合并 → 折叠过滤 → 排序 → 绑定
var collapse = _expandChanged.Select(_ =>
        new Func<PackageListItem, bool>(item => item is Header || item.Group.IsExpanded));

entries
    .Or(headers)                                   // DynamicData 无键 Or
    .Filter(collapse)                              // signal 重判折叠，不依赖 record 的 INPC
    .Sort(ItemOrderComparer)                       // (SourceOrders 档位, Header<Entry, PersistentIndex)
    .Bind(out _flatView)
    .Subscribe()
    .DisposeWith(_subscriptions);
```

- `GroupModelOf(pkg)`：`Classify(pkg.Source)` → 查 `_groupModels` 稳定字典 `GetOrAdd`；散装返回共享 `LooseGroupModel` 单例。
- `GetGroupKey(pkg)`：返回 `(Kind, source)`，散装归一个固定键。
- 折叠：toggle 命令里 `group.IsExpanded = !...; _expandChanged.OnNext(Unit.Default);` 触发全表重判折叠 filter。折叠是低频用户操作，O(n) 重判无所谓，且彻底绕开 `PackageListItem`（record）不实现 INPC 的问题。
- **头随过滤走**：headers 流派生自 `filtered`，组里包全被滤掉（含搜索零命中）时该组头也不显示。这是放弃"组头常驻显示 0/N"的直接结果——既然不显示计数，空组头无信息量。

**Sorter 成型**——"sorter 负责形状"，每个 item 自带 Group 引用，sort key 不碰 source 字符串：

```csharp
int Compare(PackageListItem a, PackageListItem b)
{
    var c = GroupOrderOf(a.Group).CompareTo(GroupOrderOf(b.Group)); if (c != 0) return c;
    c = RankOf(a).CompareTo(RankOf(b));                        // Header=0, Entry=1 → 头永远在身前
    if (c != 0) return c;
    return IntraIndexOf(a).CompareTo(IntraIndexOf(b));         // 组内按 Package.PersistentIndex
}
// GroupOrderOf: SourceOrders.IndexOf(group.Source)，散装=int.MaxValue
// RankOf:        Header→0, Entry→1
// IntraIndexOf:  Entry→Package.PersistentIndex, Header→0
```

### 3.5 搜索：纯过滤，不操纵 UI

文本搜索作用在包数据层（`Entry.Package`），就是一个 `Filter(text)` 谓词，**不写 GroupModel 任何字段**：不自动展开组、不算命中数、不显示计数。折叠是用户的事，`IsExpanded` 只由组头点击写。

后果与取舍：
- 折叠状态下的组，搜索命中其内部包时这些包不渲染（被折叠 filter 挡住）。这是"各管各事"的直接结果，可接受——组头始终可见（只要该组还有包通过过滤），用户可手动展开。
- 组里包全被滤掉（含搜索零命中）时，该组头随之不显示（§3.4 头流派生自 filtered）。
- 组头只显示：图标 + 名字 + 折叠 chevron + 「更多」菜单。不显示任何计数。

一级搜索框天然作用于底层数据，无需二级页面。

### 3.6 UI 结构：单 ItemsControl + 三模板 + 蛇形导引线

**单 `ItemsControl` + `VirtualizingStackPanel Spacing="0"`**，绑 `FlatView`。`Spacing="0"` 是导引线连续的充要条件——相邻 Entry 容器上下紧贴，左竖条拼成整线；包卡片的视觉间距改由容器内 `Margin`（如 `0,1.5`）补偿。

外层按 `PackageListItem` 的 `Header`/`Entry` 选模板；Header 模板内部再按 `GroupModel` 的具体子类（`ModpackGroupModel` / 未来的 `RecipeGroupModel`）分发不同头部外观。模板选择先用 XAML `DataType` 嵌套类型引用（`PackageListItem+Header` / `PackageListItem+Entry`，以及 Header 内按 `ModpackGroupModel` 等）；Avalonia 解析不动嵌套类型就退到 code-behind 的 `DataTemplateSelector`（按 `is Header`/`is Entry`、再按 `Group is ModpackGroupModel` 分发）。

```
┌──────────────────────────────────────────────────────┐
│ ╔═ Header(Modpack) ══════════════════════════════╗   │  独立组头卡片（蛇头）
│ ║ ▼ 📦 整合包·ATM9                         ⋯ ║   │  点击折叠；点击组头跳整合包详情；右侧「更多」菜单
│ ╚═══════════════════════════════════════════════╝   │
│ ┊┌──────────────────────────────────────────────┐   │  Entry(grouped) 模板
│ ┊│ [□] Imported Mod ATM Sapphire      @team ◯ │   │  左竖条(导引线)+缩进+InstancePackageButton
│ ┊└──────────────────────────────────────────────┘   │  上下 Margin 0,1.5 补间距，竖条紧贴拼接
│ ┊┌──────────────────────────────────────────────┐   │  Entry(grouped)
│ ┊│ [□] Imported Mod ATM Iron          @team ◯ │   │
│ ┊└──────────────────────────────────────────────┘   │
│ ╔═ Header(Recipe 未来) ═════════════════════════╗   │
│ ║ ▶ 📖 Recipe·QoL                          ⋯ ║   │  组里无包时头不显示
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
            <!-- Header 内再按 Group 子类（ModpackGroupModel/未来 RecipeGroupModel）分发 -->
            <DataTemplate DataType="PackageListItem+Header">
              <ContentControl Content="{Binding Group}">
                <ContentControl.DataTemplates>
                  <DataTemplate DataType="mod:ModpackGroupModel">
                    <app:PackageGroupHeader .../>       <!-- 图标+名字，整头可点跳详情，右侧「更多」菜单 -->
                  </DataTemplate>
                </ContentControl.DataTemplates>
              </ContentControl>
            </DataTemplate>
            <!-- Entry：左导引线列 + 缩进 + 同一个 InstancePackageButton -->
            <DataTemplate DataType="PackageListItem+Entry">
              <Grid Margin="0,1.5" ColumnDefinitions="24,*">
                <Border Grid.Column="0" Width="2" HorizontalAlignment="Center"
                        Background="{Binding 组来源色}"
                        IsVisible="{Binding Group.Kind,
                            Converter={x:Static huskui:ObjectConverters.NotMatch},
                            ConverterParameter={x:Static util:PackageSourceHelper+Kind+Manual}}"/>
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

Modpack 头部：显示整合包图标 + 名字（`ModpackGroupModel` 解析 source purl 所得字段），整头可点 → 跳转整合包详情（行为同 `InstanceSetupPage` 顶部 Reference）。

**导引线**＝相邻 grouped Entry 的左 Border 段拼接（`Spacing="0"` + 容器 `Margin="0,1.5"`），从组头正下方延伸到组末包。颜色按组来源（整合包/recipe 各一色）作为归属签名。**导引线的 hover 高亮、点击定位回首部等交互本期不做**，标 TODO——视觉先行，交互留待后续基于共享 GroupModel 设计。

**LayoutSelector 退役**：移除 `InstanceSetupPage.axaml:662` 的 TabStrip、`ViewState.LayoutIndex`、3 个 `#LayoutSelector...` 绑定；两套内联模板只保留 List（`VirtualizingStackPanel`），ItemTemplate 直接内联进 Entry 模板。`ListTemplateCombinationModel` 类不删（`MarketplaceSearchPage.axaml` 仍在用）。

样式遵循 Huskui 规范：组头是变体类（PascalCase class），展开/折叠是状态伪类（`:expanded`，见项目 AGENTS.md 样式约定）。

---

## 4. 改动面

| 层 | 文件 | 改动 |
|----|------|------|
| 模型 | `Models/PackageGroupModel.cs`（新增，抽象基类） | `Kind`/`Source`/`IsExpanded`，无命令 |
| 模型 | `Models/ModpackGroupModel.cs`（新增） | source purl 解析出的整合包元数据（名字/图标）+ 跳详情目标 |
| 模型 | `Models/LooseGroupModel.cs`（新增） | 散装虚拟组单例 |
| 模型 | `Models/PackageListItem.cs`（新增） | 拍平 union `Header`/`Entry(Package)`，都带 `Group` 引用 |
| 模型 | `Controls/PackageGroupHeader.axaml(.cs)`（新增） | 组头卡片控件：折叠触发区 + 标题 + 「更多」菜单 |
| 管道 | `PageModels/InstanceSetupPageModel.cs` | `StageView` → `FlatView`；entries/headers 双流 + Or 合并 + Subject 重判折叠；`GetGroupKey`（薄封装 `Classify`）；组级命令 `ToggleGroupEnabled`/`RemoveGroup` |
| 视图 | `Pages/InstanceSetupPage.axaml` | 扁平 `ItemsControl` → 单 `ItemsControl` + 三模板 + `VirtualizingStackPanel Spacing=0`；退役 LayoutSelector |
| 样式 | `Themes/` 或 `Controls/` | 组头 ControlTheme（变体类 + `:expanded` 伪类）；导引线来源色 |

---

## 5. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 整合包实例 | 整合包包归一组（头+身+导引线），手动包散开沉底顶格 |
| 2 | 折叠/展开组 | 点组头切换 `IsExpanded`，组内 entry 整组进出（数据层，非 IsVisible） |
| 3 | 整组启用/禁用 | 组头「更多」菜单翻转组内所有包 Enabled |
| 4 | 整组移除/解散 | Modpack 组不可整组移除（菜单禁用，管理入口是点组头跳整合包详情）；Recipe 组可解散（清 Source）；散装无整组操作 |
| 5 | 文本搜索命中 | 命中包所在组若仍展开则显示命中项；搜索不自动展开组、不写计数；组里包全未命中时该组头不显示 |
| 6 | 过滤（仅显示禁用） | 各组仅显示禁用项；组里无禁用项时该组头随之不显示 |
| 7 | recipe 包（未来） | Recipe 系统落地后自动归入 recipe 组（`RecipeGroupModel`），本期不实现 |
| 8 | 组顺序 | 整合包→recipe 默认顺序（读 `SourceOrders`）；散装永远沉底 |
| 9 | 单项操作 | 组内包与散装包用同一个 `InstancePackageButton`，启停/详情/编辑/删除与现状一致 |
| 10 | 虚拟化 | 成百上千包时筛选不卡（单 `ItemsControl` + `VirtualizingStackPanel` 生效） |
| 11 | 导引线视觉 | 相邻组内包左竖条拼接成连续长线，从组头延伸到组末包；散装无导引线 |

---

## 6. 风险与取舍

| 风险 | 取舍 |
|------|------|
| 嵌套容器破坏虚拟化 | 选**单 ItemsControl + 拍平序列**（§3.3/§3.4），放弃"组容器包裹 entry"的自然写法；代价是数据层要拍平成型 |
| 双流管道响应式正确性 | entries(`Transform`)/headers(`Group`)各自动态跟踪增删；散装增删是主要验证点。折叠重算靠 `Subject<Unit>` signal 触发全表重判，非阻断 |
| 组头随过滤消失 | 空组不保留头 | 放弃计数后空组头无信息量，头随过滤走是预期行为 |
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
- **不区分主/次 Modpack** —— Modpack 就是 Modpack，能否更新由 `CanUpdate` 控制。
- **不预先设计 RecipeGroupModel** —— recipe 组随 Recipe 系统自然出现。
- **组头不显示计数** —— 不算命中数/总数。
- **展开态不入 ViewState** —— 纯临时状态。

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

静态 `TransformMany` 会丢组成员增删（散装增删），故实际采用 entries/headers 双流 + 无键 `Or` 合并（§3.4）。拍平方案的代价是数据层要成型（sorter 保证 header 在 entry 前、filter 折叠 entry 不伤 header），换来虚拟化与完全的视觉可控。

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
