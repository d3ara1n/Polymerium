# Instance 管理视觉 polish

> 制定日期：2026-07-08
> 定位：Instance 管理的补完任务。Phase B/C 实现了功能性骨架，三处视觉细节未落地。
> 当前状态：草案
> 关联：[POLY-23](https://d3ara1n.atlassian.net/browse/POLY-23)

## 背景与动机

Phase B（主界面侧边栏重构）和 Phase C（InstancesPage）完成了功能逻辑，但设计文档里约定的三项视觉细节因"非阻塞"而被延后：

1. **Active 实例淡底色** — 主界面侧边栏中正在运行的实例（`State != Idle`）应有淡背景色强化存在感，让用户在余光中感知"哪个实例在跑"
2. **Recent 实例 ✨ 标记** — 当次会话新添加/导入的实例应带 ✨ 标记，与 Pinned 的 📌 角标直观区分
3. **标签 `+N` 截断** — `InstanceCard` 上标签超过一定数量（如 3 个）时，溢出部分收为 `+N` 药丸，保持卡片整洁

现状：

- Phase B 实施记录：`视觉强化只做了 pinned 角标，Active 淡底色 / Recent ✨ 延后`
- Phase C 实施记录：`标签 +N 截断延后（现 wrap 布局）`

## 目标

1. 主界面侧边栏 Active 实例行加淡背景色。
2. 主界面侧边栏 Recent 实例行加 ✨ 标记。
3. InstanceCard 的标签区溢出时收为 `+N` 截断。

## 非目标

- 不改 Pinned 📌 角标（Phase B 已落地且正确）。
- 不改 InstancesPage 卡片的状态显示（管理页无状态）。
- 不改主界面搜索框（Phase B 已移除）。
- 不改 InstanceEntryButton 现有状态 Tag（已有，与非 Idle 状态联动正确）。

## 核心设计

### 1. Active 淡底色

`MainWindowContext` 中 `_entries` 的 Active 行（`model.State != ModelState.Idle`）通过 `tierComparer` 已有 Tier 0 优先排序，但缺少视觉强化。

实现：给 `InstanceEntryButton`（或包裹它的容器）增加一个 `:active` 伪类或 `Active` 样式类，由 `model.State` 驱动。

```csharp
// MainWindow.axaml.cs 或绑定
// 方案 A：Style 选择器
<Style Selector="^:active">
    <Setter Property="Background" Value="{StaticResource OverlayHalfBackgroundBrush}" />
</Style>
```

或直接通过 DataTrigger 绑定 `InstanceEntryModel.State`。偏好用伪类（`:active`），参照项目约定（伪类全小写，表示运行时状态）。

### 2. Recent ✨ 标记

`InstanceEntryModel` 已有 `RecentOrder` 字段（Phase A 新增）。在列表项模板中，Recent 条目展示 ✨ 标记的位置在 Pin 📌 角标旁的「标记」区。

`MainWindow.axaml` 中实例列表的 ItemTemplate（`InstanceEntryButton` 容器）加条件显隐：

```xml
<TextBlock Text="✨"
           IsVisible="{Binding IsRecent}"
           ToolTip.Tip="{x:Static lang:Resources.InstanceEntry_RecentTooltip}" />
```

`IsRecent` 是 `InstanceEntryModel` 的 `[ObservableProperty]`，由 RecentOrder 字段 `> 0` 派生。

### 3. 标签 `+N` 截断

`InstanceCardModel.Tags` 当前渲染为 wrap 布局，标签全部展开。改为最大显示 3 个标签，超出收为 `+N`。

`Controls/InstanceCard.axaml` 中标签区：

```xml
<ItemsControl ItemsSource="{Binding TagsDisplay}">
    <!-- TagsDisplay 是 ViewModel 产出的截断列表，末项可能为 "+3" -->
</ItemsControl>
```

`InstanceCardModel` 新增：

```csharp
public const int MaxVisibleTags = 3;

public IReadOnlyList<string> TagsDisplay
{
    get
    {
        if (Tags.Count <= MaxVisibleTags) return Tags;
        var result = Tags.Take(MaxVisibleTags - 1).ToList();
        result.Add($"+{Tags.Count - MaxVisibleTags + 1}");
        return result;
    }
}
```

超出部分数量计入 `+N`，hover 或点击 `+N` 药丸时可通过 ToolTip 显示完整标签列表。

## 改动面

| 文件 | 改动 |
|------|------|
| `Models/InstanceEntryModel.cs` | 新增 `[ObservableProperty] IsRecent`（从 RecentOrder > 0 派生） |
| `MainWindow.axaml` | Active 行加 `:active` 伪类绑定；Recent 行加 ✨ 标记 |
| `MainWindow.axaml.cs` 或主题 | Active 样式的 ControlTheme/Selector 定义 |
| `Controls/InstanceCard.axaml` | 标签 ItemsControl 绑定 `TagsDisplay` |
| `Models/InstanceCardModel.cs` | `TagsDisplay` 属性 + MaxVisibleTags 常量 |
| `Properties/Resources.{resx,zh-hans.resx,Designer.cs}` | 可能新增 Recent 相关 ToolTip key |

## 验收标准

| 场景 | 期望 |
|------|------|
| 实例正在运行（部署/启动中） | 主界面侧边栏该行有淡底色强调 |
| 实例回到 Idle | 淡底色消失 |
| 新建/导入实例（未 pin） | 主界面出现 ✨ 标记 |
| 实例重启后 | Recent 清空，✨ 消失（Recent 纯进程内） |
| 标签 ≤3 个 | 全部展示，无截断 |
| 标签 >3 个 | 显示前 2 个 + 第 3 个为 `+N` |
| hover `+N` 药丸 | ToolTip 显示完整标签列表 |
