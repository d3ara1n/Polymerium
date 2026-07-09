# InstancesPage Facet 筛选聚合

> 制定日期：2026-07-08
> 定位：Instance 管理的补完任务。Phase C 的筛选 Flyout 是最小占位——固有 facet（加载器 / 版本 / 来源）已放置但缺少聚合数据（每个 facet 值有多少实例）。
> 当前状态：✅ 已实施（不作）
> 关联：[POLY-23](https://d3ara1n.atlassian.net/browse/POLY-23)

## 背景与动机

Phase C 实现了 `InstancesPage` 的筛选 flyout 骨架——点 Header 的「筛选」按钮弹 `Flyout`，内含固有 facet（加载器 / 版本 / 来源）+ 用户标签多选。但 flyout 内各 facet 值是**静态硬编码或空壳**，没有聚合计数。

用户看不到"选 Fabric 会剩下几个实例"的信息，筛选交互是盲选。补齐聚合数据后，筛选变成**可预览的**（每个 facet 值旁显示命中数），效率和信心都提升。

现状：

- `InstancesPageModel` 使用 DynamicData `SourceCache<InstanceCardModel, string>` + `.Filter(composite).SortAndBind()`
- filter 是搜索文本 + flyout facet/标签的复合谓词
- Flyout 已布局：排序 ▾ / 筛选按钮 → 弹出 Facet 区 + 标签区
- **缺聚合计数**：facet 值旁无 `(N)`

## 目标

1. 筛选 flyout 中每个 facet 值显示匹配当前过滤条件的实例计数。
2. 多选 facet 时计数联动（选 A 后 B 的计数反映"A 已选"的剩余）。
3. 性能：聚合计算在 DynamicData 管线内增量完成，不遍历全量。

## 非目标

- 不改卡片网格布局（全宽不变）。
- 不改排序机制。
- 不改搜索框行为。
- 不改标签部分（标签的多选筛选已工作，只补计数）。

## 核心设计

### 聚合数据模型

```csharp
public class FacetAggregation
{
    public required string Name { get; init; }        // "加载器" / "版本" / "来源"
    public required IReadOnlyList<FacetValue> Values { get; init; }
}

public class FacetValue
{
    public required string Label { get; init; }       // "Fabric" / "1.20.1" / "CurseForge"
    public int Count { get; set; }                    // 匹配当前过滤的实例数
    public bool IsSelected { get; set; }              // 用户是否选中
}
```

### 计算管道

在现有的 DynamicData 管线基础上增加聚合：

```csharp
// 现有：filtered = _source.Connect().Filter(enability).Filter(lockility).Filter(kind).Filter(tags).Filter(text)
// 聚合：从 filtered 流派生 facet 计数

var facetAggregations = filtered
    .TransformOnRefresh(card => new {
        Loader = card.Basic.LoaderLabel,
        Version = card.Basic.Version,
        Source = card.Basic.SourceLabel
    })
    .Group(x => x.Loader)       // 按加载器分组
    .Transform(g => new FacetValue { Label = g.Key, Count = g.List.Count })
    .Sort(SortExpressionComparer<FacetValue>.Descending(f => f.Count))
    .Bind(out _loaderFacets);
```

关键：`TransformOnRefresh` + `Group` 是增量式的——卡片进出 filtered 流时，facet 计数自动重算，无需全量遍历。

但问题是 DynamicData 的 `Group` 算子返回的是 `IGrouping`，并不是直接的计数器。替代做法：直接对 `filtered` 做订阅，在回调里手动聚合（用 LINQ 或遍历），因为 `InstancesPage` 的实例数通常在几十到几百，每次 filter 变化时手动算一次完全可接受。

```csharp
// 更简方案：filtered 流变化时手动重算 facet
filtered
    .ToCollection()                     // 变化时产出全量快照
    .Select(cards => new {
        LoaderFacets = cards.GroupBy(c => c.Basic.LoaderLabel)
            .Select(g => new FacetValue(g.Key, g.Count())),
        VersionFacets = cards.GroupBy(c => c.Basic.Version)
            .Select(g => new FacetValue(g.Key, g.Count())),
        SourceFacets = cards.GroupBy(c => c.Basic.SourceLabel)
            .Select(g => new FacetValue(g.Key, g.Count())),
    })
    .Bind(out _facetAggregations)
    .Subscribe();
```

`ToCollection` 每次 filtered 变化时产出完整列表。聚合计算本身 O(N) 但 N 在百级，选这种方案实现简单、增量正确、性能可接受。若未来实例数破千再优化。

### Flyout 内展示

```
┌─ 筛选 ──────────────────────────────────┐
│                                           │
│  加载器                                    │
│  ☑ Fabric (12)  ☐ Quilt (3)  ☐ Forge (8) │
│                                           │
│  版本                                      │
│  ☐ 1.20.1 (10)  ☑ 1.21 (5)  ☐ 1.19 (3)  │
│                                           │
│  来源                                      │
│  ☑ CurseForge (15)  ☐ Modrinth (6)       │
│                                           │
│  我的标签                                   │
│  ☑ 联机 (4)  ☐ 测试 (2)                    │
│                                           │
│                    [重置]  [应用]          │
└───────────────────────────────────────────┘
```

每个 facet 值显示 `标签 (N)`，`N` 随复合过滤条件实时联动。

## 方案

### 决策：不作

Facet 计数需要每个 MultiSelectInstanceFilter 拿到"排除自身的其他 filter 复合流"来做按值分组计数，涉及给每个 filter 单独拼 others 谓词并喂背景源。当前 Flyout 内的 ToggleButton 列表功能完整、交互直接，筛选后结果即时反映在卡片网格中——用户点 Fabric 后立即看到剩下的卡片，计数只是锦上添花。实例数通常在几十级别，筛选反馈本身已足够快。不作。
