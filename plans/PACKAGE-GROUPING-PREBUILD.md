# 包列表分组：pre-build 重构草案

> 制定日期：2026-07-07
> 定位：对已实施的 [PACKAGE-GROUPING-UI.md](PACKAGE-GROUPING-UI.md)（POLY-119）的**未来重构草案**。当前实现未实施本方案；本文档供日后改造时直接据码，不需重新推导。
> 关联：[POLY-119](https://d3ara1n.atlassian.net/browse/POLY-119)；改的是 POLY-119 已落地的管道与数据流。
> 当前状态：**草案，未实施**。

---

## 0. 为什么要改

POLY-119 落地时的设计是"反应式派生 Header"：管道从**过滤后**的包流派生出 Header（`filtered → 去散装 → GroupOn → Header`）。副作用是 **Header 跟着过滤走**——一个组在当前过滤下没有可见成员时，组头也随之消失。

但这**不符合原始意图**。原始意图是：

> **组头是组头，组成员是组成员。组头在视觉上不是列表的一部分，它代表"这个外部引用存在"，应当独立于过滤/搜索。** 搜索时即便某组没有命中项，组头也该照常显示（让用户看到完整的组结构）。

所以正确的行为是：**组头只要该组在数据里存在就常驻显示，不受包过滤影响**；过滤只作用于成员（Entry）。当前实现的 filter-tracking 是偏差，本草案把它纠正过来。

---

## 1. 与现状的差异（一句话）

| 维度 | 现状（反应式派生） | 目标（pre-build） |
|------|------|------|
| source 存什么 | `InstancePackageModel`（包） | `PackageListItemBase`（Header+Entry，拍平好的项） |
| 谁建 Header | 管道（从过滤后的包派生） | Merge-Load（从**全量**包构建，与过滤无关） |
| Header 受过滤影响吗 | **是**（空组头消失） | **否**（常驻，只要组存在） |
| 管道职责 | 派生 entries+headers、Or 合并、过滤、折叠、排序 | 仅过滤 Entry + 折叠 + 排序（Header 永远放行） |

---

## 2. 目标设计

### 2.1 source 直接存拍平的项

`_stageSource` 从 `SourceCache<InstancePackageModel, Profile.Rice.Entry>` 改为存 `PackageListItemBase`。keyed 或 unkeyed 二选一：

- **`SourceCache<PackageListItemBase, ItemKey>`**（推荐）：key 是判别键——Header 用组键 `(Kind, Source)`、Entry 用包键 `Entry`（引用地址）。增删按 key 干净。
- `SourceList<PackageListItemBase>`（备选）：无键，更简单，但增删要自己管理。

`ItemKey` 形如：
```csharp
public abstract record PackageListItemKey
{
    public sealed record Group(PackageSourceHelper.Kind Kind, string? Source) : PackageListItemKey;
    public sealed record Package(Profile.Rice.Entry Entry) : PackageListItemKey;  // Entry 是 class，按地址
}
```

### 2.2 Merge-Load 构建拍平序列

`TriggerPackageMerge` 算完包 diff 后，**重建整条拍平序列**（全量，与过滤无关）：

```csharp
// 全量包 → 每个包一个 Entry；按 source 去重 → 每个非散装组一个 Header
var items = new List<PackageListItemBase>();
var seenGroups = new HashSet<string>();   // 已产出 Header 的 source
foreach (var pkg in <current packages>)
{
    var group = GroupModelOf(pkg);        // 复用 _groupModels，IsExpanded 不丢
    // 该组第一次出现 → 先放 Header（头在身前，sorter 也保证，但显式产出更直观）
    if (pkg.Entry.Source is { } src && seenGroups.Add(src))
        items.Add(new PackageListItemBase.Header { Group = group });
    items.Add(new PackageListItemBase.Entry { Group = group, Package = pkg });
}
// 全量替换 source（Edit: clear + add，或按 key 增删）
_source.Edit(inner => { inner.Clear(); inner.AddOrUpdate(items); });
```

要点：
- **GroupModel 实例跨重建稳定**（`_groupModels` 字典 + `EnsureModpackGroup`），所以重建不会丢 `IsExpanded`、不会重跑组信息加载。
- **Entry 包裹的是同一个 `InstancePackageModel` 引用**（包信息异步加载、`IsEnabled` 翻转等都走它自己的 INPC，无需替换 Entry 项）。
- Header 是否产出取决于**数据里该组有没有包**，与当前过滤无关——这正是要的。

### 2.3 信息加载（与现状一致，只是时机更自然）

Merge-Load 阶段一个 pass 内：
- `RefreshPackagesAsync`：包 `Info`（已有，不改）。
- `RefreshGroupsAsync`：组 `Name`/`Thumbnail`（已有，批量 `ResolvePackagesAsync`，不改）。

两者都写进**已被 source 引用的 model 实例**（`InstancePackageModel.Info` / `ModpackGroupModel.Name`），UI 通过共享引用自动更新。**不再需要管道去派生 Header、也不需要 entries/headers 两条流。**

### 2.4 管道：只过滤 Entry + 折叠 + 排序

```csharp
// 包过滤谓词（enability/lockility/kind/tags/text）仍是 Func<InstancePackageModel, bool>，
// 在管道里包一层：Header 永远放行，Entry 才套包过滤
var packageFilters = ...;  // IObservable<Func<InstancePackageModel,bool>> 合并（或保留现有逐个 .Filter）
_source.Connect()
    .Filter(item => item is PackageListItemBase.Header
                  || (item is PackageListItemBase.Entry e && packageFilter(e.Package)))
    .AutoRefreshOnObservable(item => item.Group.WhenPropertyChanged(g => g.IsExpanded))
    .Filter(item => item is PackageListItemBase.Header || item.Group.IsExpanded)  // 折叠：组头仍放行
    .Sort(comparer)   // PackageListItemComparer 不变（SourceOrders 档位, Header<Entry, PersistentIndex）
    .Bind(out _flatView);
```

- **包过滤**：只过滤 Entry；Header 永远留 → **组头不受搜索/过滤影响**。
- **折叠**：折叠只藏该组的 Entry，Header 仍显示（点组头能展开看成员）。
- `PackageListItemComparer`、`PackageListItemTemplateSelector`、`ModpackGroupContentTemplate` 等**不用改**。

### 2.5 数据模型

`PackageListItemBase` / `GroupModelBase` / `ModpackGroupModel` / `LooseGroupModel` **结构不变**——改的只是"谁负责产出 Header 项、何时产出"，不是模型本身。

---

## 3. 改动面

| 文件 | 改动 |
|------|------|
| `PageModels/InstanceSetupPageModel.cs` | `_stageSource` 类型改 `PackageListItemBase`；`TriggerPackageMerge` 重建拍平序列；`OnInitializeAsync` 管道去掉 entries/headers 双流与 `Or`，改单流 `.Filter(Entry)` + 折叠 + 排序；`GetGroupKey`/`GroupModelOf`/`EnsureModpackGroup`/`RefreshGroupsAsync` 保留 |
| 其它（模型/选择器/comparer/axaml/ControlTheme） | **基本不动** |

---

## 4. 取舍 / 注意

- ** gained**：组头独立于过滤（原始意图）；管道从"双流派生"简化为"单流过滤"；Header 的产出时机清晰（Merge 时确定，不随过滤抖动）。
- **代价**：Merge 要重建整条拍平序列（但项是薄包装，GroupModel/InstancePackageModel 实例稳定，重建廉价；`IsInfoLoaded`/`IsExpanded` 都不丢）。
- **要验证**：
  - 组里**最后一个包被删**时，Header 要随之消失（重建时该组无包 → 不产出 Header）。
  - 组里**第一个包出现**时，Header 要出现。
  - 折叠/搜索/计数器（`FilteredCount`）行为：`FilteredCount` 仍只数 Entry（包过滤后的包数），不受 Header 常驻影响——语义不变。
  - 散装包（`LooseGroupModel`）仍无 Header、无导引线、沉底——不变。
- **不依赖 recipe**：`RecipeGroupModel` 仍留待 Recipe 系统；本重构不涉及。

---

## 5. 不做的事

- 不改 `PackageSourceHelper`（分组依据）。
- 不改 `SourceOrders`（组间顺序，POLY-116）。
- 不改组级「更多」菜单、导引线交互、按来源分色（这些是 POLY-119 留待后续的，与本重构正交）。
