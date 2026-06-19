# 实例配置页面的依赖图与标签图（POLY-86 / POLY-87）

> **状态**: POLY-86 ✅ 已完成；POLY-87 ⏳ 未开始（复用 POLY-86 控件）
> **关联 Issue**: [POLY-86](https://d3ara1n.atlassian.net/browse/POLY-86)（依赖图表）、[POLY-87](https://d3ara1n.atlassian.net/browse/POLY-87)（标签图表）
> **分支**: `feature/POLY-86-dependency-graph`

---

## 背景与动机

两个 issue 都曾在 Jira 上被打上「短期内无法实现」标签，原因是 Avalonia 生态里成熟的图（节点-边）可视化库一直缺失 —— `GraphX` / `GraphSharp` 都是 WPF 时代的库，无法在 Avalonia 用；自研布局算法成本高。

经联网查证（2026-06），现状已变：存在可直接使用的方案，这个判断可以推翻。

### 两个 issue 的本质

| Issue | 数据 | 关系形态 | UI 占位 |
|-------|------|----------|---------|
| **POLY-86** 依赖图 | 已安装包之间的 `Dependency` 关系 | DAG（有向图） | ✅ `InstanceSetupPage.axaml` 依赖图菜单项（已激活） |
| **POLY-87** 标签图 | `Profile.Rice.Entry.Tags` + `Project.Tags` | 多对多（标签 ↔ 包） | ❌ 无 |

两者关系结构相近，**共用一套图控件**（`AvaloniaGraphControl` 的 GraphPanel + 自研 `ZoomView` 容器）。POLY-86 把基础设施打好，POLY-87 套数据即可。

---

## 选型决策（已落地）

### 路线 A（采用）：引入 `AvaloniaGraphControl`

| 维度 | 结论 |
|------|------|
| **机制** | 自定义 `GraphPanel`（继承 Panel）+ 内部调用 **MSAGL** 做布局 |
| **License** | ✅ **MIT**（含 MSAGL 的 MIT 继承） |
| **版本** | 0.7.0 |
| **目标框架** | netstandard2.1 |
| **项目实际** | Avalonia 12.0.4 + .NET 10 ✅ 已验证运行正常 |
| **维护** | 332 stars；作者声明「仅维护模式，只接 bug fix 和版本升级 PR」 |

### 不可用方案（已排除）

- ❌ `GraphX` / 原版 `GraphSharp` —— 纯 WPF
- ❌ `Avalonia Pro` 的 Force-Directed Graph / FlowChart —— 商业付费
- ❌ `GoDiagram` / `MindFusion` —— 商业闭源

---

## POLY-86 实际实现（已完成）

### 架构总览

```
InstanceSetupPage 菜单项
  → InstanceSetupPageModel.GotoDependencyGraphCommand
  → overlayService.PopModal<InstanceDependencyGraphModal>(Basic)
  → SimpleViewActivator 配对 ModalModel，注入 DI
  → InstanceDependencyGraphModalModel.OnInitializeAsync
      → ProfileManager.TryGetImmutable(Basic.Key).Setup.Packages
      → PackageHelper.TryParse(entry.Purl) → PackageIdentifier 批量
      → DataService.ResolvePackagesAsync(batch, Filter.None)   // 拿 Package + Dependencies
      → 只连「指向另一个已安装包」的依赖边，孤儿包自然不出现
      → Graph (SugiyamaScheme)
  → Modal: ZoomView 满铺 + GraphPanel + 内置 QoL 控件 + 左下统计卡片
```

### 关键文件

| 文件 | 职责 |
|---|---|
| `Modals/InstanceDependencyGraphModal.axaml(.cs)` | husk:Modal，ZoomView 满铺 + 浮动关闭按钮 + 左下统计卡片 |
| `ModalModels/InstanceDependencyGraphModalModel.cs` | 注入 ProfileManager/DataService，构建图 + 统计 |
| `Models/DependencyGraphNode.cs` | `record(Key, Label)`，Key=(Label,Namespace,ProjectId) |
| `Controls/ZoomView.cs` + `ZoomView.axaml` | 自研有界缩放容器（含 ControlTheme） |
| `Controls/ZoomMinimap.cs` | 自绘小地图（示意性，非缩略图） |

### 数据范围（关键设计）

**只画已安装包之间的依赖关系**：
- 不拉取网络上的依赖项（不二次 QueryProjectsAsync）
- 只在已安装包节点字典里连「指向另一个已安装包」的依赖边
- GraphPanel 从 Edges 推导节点集合 → 孤儿包（既不依赖也不被依赖）自然不出现
- 自环跳过

### 统计信息

ModalModel 暴露 `TotalPackages` / `VisiblePackages` / `HiddenPackages`（孤儿）/ `EdgeCount`，Modal 左下角卡片显示。构建时用 `connected` HashSet 记录参与边的节点 key。

---

## ZoomView（自研缩放容器）

弃用了 `SmoothScroll.Avalonia`（滚轮不滚动、拖拽回弹）和 `PanAndZoom`，自研 `Controls/ZoomView`。

### 核心机制

- **ContentControl + ControlTheme**：模板内 Border(TemplateBinding Background 命中层) + Panel 叠层 ContentPresenter + 水平/垂直 ScrollBar + 集中 QoL 控件区
- **有界画布**：画布 = Content 自然尺寸（GraphPanel 的 MSAGL 布局尺寸）。`MeasureOverride` 用 `Size.Infinity` measure Content 拿真实尺寸（关键：base.ArrangeOverride 会用视口尺寸重新 measure 截断 DesiredSize，故 `_contentSize` 只由 MeasureOverride 维护）
- **Matrix 变换**：单 Matrix 表达缩放+平移，RenderTransform 设在 **Content** 上（不是 ContentPresenter——后者 Bounds 是视口大小会裁掉超出内容）。变换后像素由 ZoomView ClipToBounds 裁到视口
- **平移 clamp**：缩放后 > 视口的维可滚动（夹在 `[vp-scaled, 0]`），≤ 视口的维居中
- **滚轮缩放**：以鼠标位置（内容坐标）为锚点
- **中键拖拽**：delta 除以当前缩放（内容坐标跟手，缩小省力放大精细）

### 两种 fit 语义

| 操作 | 公式 | 效果 |
|---|---|---|
| **自适应 FitToContent** | `max(vp.W/cw, vp.H/ch)` cover fit | 短维充满视口，长维溢出可滚动，缩放不致太小看得清。溢出维居中 |
| **全视图 FitToAll** | `min(vp.W/cw, vp.H/ch)` contain fit | 整图可见，较短维充满、较长维留白居中 |
| **缩放下限 EffectiveMinZoom** | contain fit | 滚轮缩到此（整图可见）即停，再缩无意义 |

### 内置 QoL 控件（右下角集中区）

竖直容器：**小地图** + **一排**（`-` [缩放滑条] `+` [全视图] [自适应]）。
- **滚动条**：双向同步（Maximum=scaledContent-viewport，Value=-M31/-M32），`_suppressScrollSync` 防递归，不可滚时隐藏
- **缩放滑条**：`Zoom` StyledProperty 双向，外部设定以视口中心缩放；`EffectiveMinZoomValue` 供 Minimum 绑定；`_suppressZoomSync` 防递归
- **按钮命令**：直接绑 public 方法名（ZoomIn/ZoomOut/FitToContent/FitToAll），用 `{Binding Method, RelativeSource={RelativeSource TemplatedParent}}`
- **小地图**：独立 `ZoomMinimap` 自绘 Control（参考 DiffOverviewBar），contain fit 示意画布 + 可视框，拖动映射回画布坐标平移

### 浮动层布局

- 右上：关闭按钮（AccountEntryModal 同款：Small + FullCornerRadius + Dismiss 图标）
- 右下：ZoomView 内置集中控件区
- 左下：包数量统计卡片（总包数 / 已显示 / 未显示 / 依赖关系）
- 水平滚动条右 Margin、垂直滚动条底 Margin 给集中区让位

---

## POLY-87 标签图（未开始）

复用 POLY-86 的 GraphPanel + ZoomView：

- 图类型：二分图（标签节点 ↔ 包节点，边表示归属）
- 数据源：`Profile.Rice.Entry.Tags`（用户自定义）+ `Project.Tags`（仓库分类）
- 布局：力导向（MSAGL `Ranking` 或 `MDS`）比分层更适合多对多关系
- 入口：需新增（POLY-86 有现成菜单项，POLY-87 没有）
- 可复用 ZoomView 全部 QoL（滚动条/小地图/缩放/fit），只需换 Graph 数据源和 ModalModel

---

## 后续可选优化（非阻塞）

- **节点样式美化**：按 `IsRequired` 区分软/硬依赖边样式（虚线/实线、不同颜色），根节点（被依赖多）与叶节点视觉区分
- **边路由**：横平竖直 + 同结构线条合并（需 fork AvaloniaGraphControl 或自写算法，200-1000 行；AvaloniaGraphControl 0.7.0 零暴露 EdgeRoutingSettings）
- **本地化**：modal 内硬编码文案（小地图/统计卡片标签等）抽到 `Resources.resx` + `Resources.zh-hans.resx` + `Resources.Designer.cs`

---

## 进度日志

- **2026-06-19** 调研完成，确认两个 issue 都可实现；用户拍板路线 A + 先做 POLY-86
- **2026-06-19** 核实 License（MIT）+ 起 POC 分支
- **2026-06-19** 加包 `AvaloniaGraphControl 0.7.0`，NuGet 解析 + 全量 build 通过
- **2026-06-19** 边路由调研结论：AvaloniaGraphControl 0.7.0 无暴露点，横平竖直需 fork，母线合并需自写算法。用户决定暂搁置
- **2026-06-19** 弃用 SmoothScroll.Avalonia（滚轮不滚动、拖拽回弹），自研 ZoomView
- **2026-06-19** 从 Subpage 方案改为 Modal 方案
- **2026-06-19** 接入真实数据 + ModalModel 化：`PopModal<InstanceDependencyGraphModal>(Basic)` 注入 ProfileManager/DataService
- **2026-06-19** 收敛数据范围：只画已安装包之间的依赖，孤儿包不出现；`DependencyGraphNode` 简化为 `record(Key, Label)`
- **2026-06-19** ZoomView 有界画布：用 `Size.Infinity` measure 拿真实画布尺寸（修复 base.ArrangeOverride 截断 DesiredSize 导致只显示左上角的 bug）；动态缩放下限 = contain fit
- **2026-06-19** 自适应（cover fit，溢出维居中）+ 全视图（contain fit）按钮；关闭按钮换 AccountEntryModal 同款样式
- **2026-06-19** ZoomView 内置滚动条（ControlTheme + 双向同步 + 防递归）
- **2026-06-19** 缩放滑条 + 右下集中控件区（小地图占位 + 滑条 ±按钮 + 全视图/自适应按钮），全 QoL 控件内置进 ZoomView
- **2026-06-19** ZoomMinimap 自绘小地图（示意性，参考 DiffOverviewBar 二维化）
- **2026-06-19** 左下角包数量统计卡片（总包数/已显示/未显示/依赖关系）
- **POLY-86 完成**，待写 changelog 与合并
