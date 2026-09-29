# InstancesPage 维度分组

> 制定日期：2026-07-08
> 回填日期：2026-09-29 —— 已实施并验收
> 关联：[POLY-23](https://d3ara1n.atlassian.net/browse/POLY-23)

## 结果

实例页支持按固定维度分组浏览：不分组 / 加载器 / 游戏版本 / 上次游玩。组由数据动态生成，空组随过滤消失；组头显示名称与计数、点击折叠；组数为一时组头自动隐藏——不分组即常量键单组，机制上与其余维度完全一致。

## 实施记录

- 维度选择持久化于 Configuration 键 `Application.Interface.InstancesPage.Grouping`（int，默认 0 不分组）；实例页全局唯一，属软件偏好而非实例数据。
- `Models/InstanceGroupModel.cs`：组模型持有组键（`Label`，资源键或字面量）、组内卡片集合（构造时对 DynamicData `IGroup.Cache` 做 `SortAndBind`，排序器沿用现有排序下拉）、`IsExpanded`、`ShowHeader`；实现 IDisposable，由管道 `DisposeMany` 在组消失时回收内层订阅。
- `InstancesPageModel`：管道为 `_cards.Connect().Filter(combined).Group(GetGroupKey).Transform(...).DisposeMany().Sort(组序).Bind(Groups)`；维度切换与排序切换同样走重建管道，搜索/筛选作用在分组之前。
- 组键：加载器用 `LoaderHelper` 显示名（无加载器归 `Enum_Vanilla`，组序原版置顶、其余按名）；游戏版本用原始版本串（`Version` 语义降序，解析失败退字符串降序）；上次游玩分固定桶 今天 / 本周（7 天内）/ 本月 / 更早 / 从未游玩（按桶序倒排）；不分组为常量空串键。
- `ShowHeader` 规则：`Groups.CollectionChanged` 时统一置 `Count > 1`，无任何维度特判；订阅挂在 `Subscribe()` 之前，初始填充即生效。
- XAML：外层 ItemsControl 渲染组（组头 = Ghost Button + `SwitchContainer` chevron + `LocalizedKey` 标签 + `Cards.Count` 计数，样式对齐包管理页组头），内层 ItemsControl 为原 FlexWrapPanel 卡片流原样搬入；折叠 = 组头命令翻转 `IsExpanded`，内层 `IsVisible` 绑定。未采用包管理页的单 ItemsControl 拍平方案——实例页本无虚拟化，嵌套容器即可。
- 展开态不持久化。
