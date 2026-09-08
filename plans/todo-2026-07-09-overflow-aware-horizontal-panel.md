# 溢出感知水平 Panel + ItemsControl 适配

> 制定日期：2026-07-09
> 定位：自定义 Avalonia Panel，内容超出容器宽度时自动截断，第 n+1 项起不可见并由配套 ItemsControl 在外部显示 `+N`。
> 当前状态：草案

## 背景与动机

`InstanceCard` 的标签区当前用 `WrapPanel` 自动换行，行为正确但没有截断能力。多处 UI 都有"标签行留一行、溢出用 +N 收起"的需求——不仅是 InstanceCard，未来筛选栏、详情页标签行都适用。

Avalonia 内置的 `StackPanel` / `WrapPanel` / `VirtualizingStackPanel` 都不做内容感知的溢出隐藏：它们要么全渲染（StackPanel 超出被 clip），要么换行（WrapPanel），没有"算到第几项放得下就停，把剩下的数量编码为 +N 交给 ItemsControl"的逻辑。

需要一个可复用的 Panel 控件来干净解决。

## 目标

1. 实现一个 `OverflowTruncatingPanel : Panel`，只做水平方向排列（类似 StackPanel 但截断）。
2. Panel 在 `MeasureOverride` 中按从左到右测算每个子项；累积宽度超出可用宽度时停止测量，后续子项设为空（Size.Empty）。
3. Panel 仅管布局与截断，不负责 +N 渲染——+N 由外部 `ItemsControl` 根据 `OverflowCount` 产出。

## 非目标

- 不做垂直溢出版本（需求全是水平标签行）。
- 不做可折叠/展开切换（仅截断，不做展开按钮）。
- 不做动画过渡。

## 核心设计

### Panel 行为

```
容器宽度 200px

[Fabric    ] [1.20.1   ] [CurseForge]   → 三项都能放下 → 无溢出
[Forge     ] [1.21     ] [Modri... ] ／  → 第4项放不下 → 截断，前三项显示


MeasureOverride 伪代码：

availableWidth = constraint.Width
accumulated = 0
visibleChildren = 0

foreach child in Children:
    child.Measure(constraint with infinite width)
    if accumulated + child.DesiredSize.Width > availableWidth
       break
    accumulated += child.DesiredSize.Width
    visibleChildren++
    // remaining children: Measure with Size.Empty to suppress

剩余子项数 = Children.Count - visibleChildren
```

### ItemsControl 适配

Panel 负责截断布局，`ItemsControl` 配合：

```xml
<Grid>
    <!-- 主 ItemsControl：绑定全部数据，用 OverflowTruncatingPanel 当 ItemsPanel -->
    <ItemsControl ItemsSource="{Binding AllTags}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <local:OverflowTruncatingPanel />
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
    </ItemsControl>

    <!-- +N 溢出标记：放在外部，绑 OverflowCount -->
    <Border IsVisible="{Binding OverflowCount, Converter={x:Static NumberConverters.IsNonZero}}">
        <TextBlock Text="{Binding OverflowCount, StringFormat=+'{}'}" />
    </Border>
</Grid>
```

这要求 Panel 把被截掉的子项数量 **传出**，在数据不参与截断时（像 Previewer 这种复杂的），`+N` 直接绑 ViewModel 属性（因为提前知道了要几个）。

## 改动面

- 新建 `Controls/OverflowTruncatingPanel.cs`（纯 Avalonia Panel，不依赖项目模型）。
- 仅在需要 `+N` 的地方配合使用，不改现有的任意布局。
