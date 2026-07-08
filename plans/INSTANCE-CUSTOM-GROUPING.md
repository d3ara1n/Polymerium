# InstancesPage 自定义分组

> 制定日期：2026-07-08
> 定位：Instance 管理的补完任务。Phase A/B/C 未做自定义分组规则与分组编辑 UI。
> 当前状态：草案
> 关联：[POLY-23](https://d3ara1n.atlassian.net/browse/POLY-23)

## 背景与动机

`InstancesPage` 的实例列表是**全量扁平卡片网格**，只有搜索/筛选做过滤，没有分组概念。用户有几十个实例时，希望按自己定义的分组归类展示，例如：

- "CLient-side" / "Server-side"
- "Under Development" / "Published"
- "With friends" / "Solo"

筛选器虽然能做到类似效果（选标签 = 看这一类），但筛选是**临时态**——切换 tag 后列表只显示该类，想看其他的要切 tag 或重置。分组是**常驻结构**——所有实例按组铺在同一个视口里，同时看到多个组，组内可看详情、可折叠。

现状：

- 标签（`Tags`）提供主观分类维度，能在筛选 flyout 选标签过滤
- 搜索/筛选是纯过滤，分组 UI 不存在
- `InstanceCard` 没有组头概念

## 目标

1. 用户可创建自定义分组规则（按标签包含/加载器/版本来归类）。
2. 分组后 InstancesPage 卡片网格按分组展示，每组有组头标签 + 折叠。
3. 分组规则持久化（存 PersistenceService 或 profile 无关的轻量存储）。
4. 不匹配任何分组的实例归入「未分组」区域。

## 非目标

- 不做手动拖拽分组（用户拖实例进某一组）——用规则自动归类更符合管理页定位，手动拖后续考虑。
- 不改 `InstanceCard` 本身。
- 不影响主界面侧边栏的 P/A/R 三层（那是瞭望视图，不是管理视图）。
- 不与部署优先级耦合（分组是 UI 管理属性，不是包分组那种部署语义）。

## 核心设计

### 分组规则模型

```csharp
public class InstanceGroupRule
{
    public required string Id { get; init; }              // 分组唯一标识
    public required string Name { get; set; }              // 展示名
    public string? Color { get; set; }                     // 组头色标
    public required GroupCondition Condition { get; init; } // 匹配条件
}

public class GroupCondition
{
    public GroupMatchKind Kind { get; init; }              // TagContains / LoaderEquals / VersionMatches
    public required string Value { get; init; }             // 如 "联机" / "fabric" / "1.20.*"
}
```

首版只支持简单的**单条件**匹配（标签包含、加载器等于、版本通配），不做多条件的 AND/OR/NOT 组合——复杂度先收敛。条件按顺序匹配，实例命中第一个分组的规则后不再检查后续分组。

### 分组管理 UI

Group 管理入口：`InstancesPage` 顶栏筛选按钮旁新增「分组」按钮或 Tab。

```
┌─ 分组管理 ──────────────────────────────────────┐
│                                                   │
│  当前分组                                         │
│  [联机] [单机] [开发中] [+ 添加分组]              │
│                                                   │
│  编辑「联机」                                     │
│  ┌─────────────────────────────────────────────┐  │
│  │ 名称: 联机                                  │  │
│  │ 条件: 标签包含    [联机 ___ ▼]              │  │
│  │ 色标: ● 蓝 ● 绿 ● 橙 ○ 紫                  │  │
│  └─────────────────────────────────────────────┘  │
│                                                   │
│                   [保存]     [删除此分组]          │
└───────────────────────────────────────────────────┘
```

### 列表渲染

`InstancesPage` 卡片网格从「全量无序」改为「按组分区」：

```
┌─ 联机（4）─ [+ 折叠] ──────────────────────────┐
│  [卡] [卡] [卡] [卡]                              │
└──────────────────────────────────────────────────┘
┌─ 单机（7）─ [+ 折叠] ──────────────────────────┐
│  [卡] [卡] [卡]                                   │
│  [卡] [卡] [卡] [卡]                              │
└──────────────────────────────────────────────────┘
┌─ 未分组（2）─ [+ 折叠] ───────────────────────┐
│  [卡] [卡]                                       │
└──────────────────────────────────────────────────┘
```

每个分区由组头 + 卡片网格组成，支持折叠。分区之间的排序由规则列表顺序决定，未分组永远沉底。

### 实现方案

不改变现有的 `SourceCache<InstanceCardModel, string>` 结构。在 `InstancesPageModel` 增加分组层后，ViewModel 产出分组的拍平序列：

```csharp
// 拍平为单个序列供 ItemsControl 渲染（类似 PACKAGE-GROUPING-UI 的双流思路）
private readonly SourceCache<GroupedItem, string> _groupedSource = new(x => x.Key);

// 实例变动 + 分组规则变动 → 重算分组归属
void Repartition()
{
    var groups = _groupRules.Select(r => new GroupHeader(r)).ToList();
    var ungroupedCards = _cards.Where(c => !groups.Any(g => g.Matches(c))).ToList();

    var items = new List<GroupedItem>();
    foreach (var g in groups)
    {
        items.Add(new GroupedItem.Header(g));
        items.AddRange(g.MatchedCards.Select(c => new GroupedItem.Entry(c)));
    }
    items.Add(new GroupedItem.Header(UnGrouped));
    items.AddRange(ungroupedCards.Select(c => new GroupedItem.Entry(c)));

    _groupedSource.EditDiff(items);  // 增量刷新
}
```

使用动态拍平（与 `PackageGroupItem` 类似思路）避免嵌套容器破坏虚拟化。

## 改动面

| 文件 | 改动 |
|------|------|
| `Models/InstanceGroupRule.cs`（新增） | 分组规则定义 + GroupCondition |
| `Models/GroupedItem.cs`（新增） | 拍平 union Header/Entry |
| `Services/PersistenceService.cs` | 新增分组规则持久化（复用 WidgetLocalSection 机制） |
| `PageModels/InstancesPageModel.cs` | 分组管理状态 + Repartition 逻辑 + 组级折叠 |
| `Pages/InstancesPage.axaml` | 卡片网格按组分段 + 组头 + 折叠；新增分组管理入口 |
| `Controls/InstanceGroupHeader.axaml(.cs)`（新增） | 组头控件：色标 + 名称 + 计数 + 折叠 |
| `Dialogs/GroupRuleEditorDialog.axaml(.cs)`（新增） | 创建/编辑分组规则的对话框 |
| `DialogModels/GroupRuleEditorDialogModel.cs`（新增） | 分组编辑逻辑 |
| `Properties/Resources.{resx,zh-hans.resx,Designer.cs}` | 分组相关文案 |

## 验收标准

| 场景 | 期望 |
|------|------|
| 无分组规则 | 所有实例在「未分组」区，列表与现行为一致 |
| 创建规则"标签包含 联机" | 带"联机"标签的实例自动归入「联机」组，组头显示计数和色标 |
| 创建多条规则 | 实例按规则顺序匹配，命中首个即归入 |
| 折叠/展开组 | 点组头切换 |
| 删除分组规则 | 实例自动回「未分组」 |
| 规则修改后实例列表实时刷新 | 改规则条件 → 重算分组 |
| 搜索/筛选与分组共存 | 搜索/filter 作用于底层数据，分组反映过滤后的结果 |
