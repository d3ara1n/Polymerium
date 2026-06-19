# 实例配置页面的依赖图与标签图（POLY-86 / POLY-87）

> **状态**: POC 进行中（AvaloniaGraphControl 兼容性验证阶段）
> **关联 Issue**: [POLY-86](https://d3ara1n.atlassian.net/browse/POLY-86)（依赖图表）、[POLY-87](https://d3ara1n.atlassian.net/browse/POLY-87)（标签图表）
> **分支**: `feature/POLY-86-dependency-graph`
> **先做**: POLY-86（依赖图）；POLY-87 复用同一套控件

---

## 背景与动机

两个 issue 都曾在 Jira 上被打上「短期内无法实现」标签，原因是 Avalonia 生态里成熟的图（节点-边）可视化库一直缺失 —— `GraphX` / `GraphSharp` 都是 WPF 时代的库，无法在 Avalonia 用；自研布局算法成本高。

经联网查证（2026-06），现状已变：存在可直接使用的方案，这个判断可以推翻。

### 两个 issue 的本质

| Issue | 数据 | 关系形态 | UI 占位 |
|-------|------|----------|---------|
| **POLY-86** 依赖图 | `Package.Dependencies` / `InstancePackageVersionModel.Dependencies` 已就绪 | DAG（有向图） | ✅ `InstanceSetupPage.axaml:703` 已有禁用菜单项 `IsEnabled="False"` |
| **POLY-87** 标签图 | `Profile.Rice.Entry.Tags` + `Project.Tags` 已就绪 | 多对多（标签 ↔ 包） | ❌ 无 |

两者关系结构相近（都是多对多有向），**可共用一套图控件**。做第一个时把基础设施打好，第二个套数据即可。

用户对 POLY-87 的设想（原话提炼）：只读界面，能看到所有标签以及对应的包，类似依赖树；能缩放查看；做一个 Panel 负责给 Cell 布局，Panel 塞 ViewBox 里。

### 关键修正

Avalonia 的 `Viewbox` **只做静态 Uniform 缩放，不支持鼠标滚轮缩放/拖拽平移**。要交互缩放须用 `PanAndZoom` 的 `ZoomBorder`，或在最终方案里走内置缩放。

---

## 选型决策（已确认）

### 路线 A（采用）：引入 `AvaloniaGraphControl`

| 维度 | 结论 |
|------|------|
| **机制** | 自定义 `GraphPanel`（继承 Panel）+ 内部调用 **MSAGL** 做布局 |
| **License** | ✅ **MIT**（含 MSAGL 的 MIT 继承），仓库 LICENSE 文件确认 |
| **最新版** | 0.7.0（2025-12-02） |
| **目标框架** | netstandard2.1 |
| **声明依赖** | Avalonia `>= 11.3.9` |
| **项目实际** | **Avalonia 12.0.4 + .NET 10** ⚠️ 需 POC 验证 |
| **维护** | 332 stars；作者声明「仅维护模式，只接 bug fix 和版本升级 PR」 |

之所以选 A 而非自研（路线 B/C）：A 本质就是用户设想（自定义 Panel + 布局算法）的开源成品，节点是 Avalonia 普通控件、支持 DataTemplate、内置缩放，最契合方向，落地最快。

### 备选路线（POC 失败时回退）

- **B**: `Microsoft.Msagl`（纯算法，MIT）+ 自写 `GraphPanel` + `PanAndZoom`
- **C**: `GraphShape`（纯算法，MIT，2022 停更）+ 自写 Panel + `PanAndZoom`

### 不可用方案

- ❌ `GraphX` / 原版 `GraphSharp` —— 纯 WPF
- ❌ `Avalonia Pro` 的 Force-Directed Graph / FlowChart —— 商业付费
- ❌ `GoDiagram` / `MindFusion` —— 商业闭源
- ⚠️ `Nodely`（2026-06 极新，license 不明）—— 暂不考虑

---

## POC 验证清单

POC 的目标是回答「路线 A 能不能在本项目跑起来」。验证矩阵：

| # | 验证项 | 状态 | 备注 |
|---|--------|------|------|
| 1 | NuGet 解析（Avalonia 12 兼容） | ✅ 通过 | `Package 'AvaloniaGraphControl' is compatible with all the specified frameworks` |
| 2 | 引入包后全量 build 不破坏现有项目 | ✅ 通过 | `Build succeeded. 0 Error(s)`（仅原有 SQLitePCLRaw 漏洞警告） |
| 3 | `GraphPanel` 在 Avalonia 12 运行时能正常布局 | ⏳ 待人眼验证 | 代码已写，编译通过；运行时渲染需实际启动应用确认 |
| 4 | MSAGL 布局算法在 .NET 10 下工作 | ⏳ 待人眼验证 | 同上 |
| 5 | 节点用 DataTemplate 自定义、继承主题样式 | ⏳ 待人眼验证 | DataTemplate 用 `ControlAccentTranslucentHalfBackgroundBrush` 等主题资源 |
| 6 | 内置缩放/拖拽可用 | ⏳ 待人眼验证 | 改用 SmoothScroll.Avalonia 的 ScrollView（Composition 层缩放），非 PanAndZoom |

---

## 新增 `InstanceDependencyGraphPage` 的最小步骤

（POC 阶段按此执行；explorer 已给出精确路径）

| # | 操作 | 文件 |
|---|------|------|
| 1 | 创建 View | `Pages/InstanceDependencyGraphPage.axaml`（`<app:Subpage>` 根节点，`x:DataType=InstanceDependencyGraphPageModel`） |
| 2 | 创建 code-behind | `Pages/InstanceDependencyGraphPage.axaml.cs`（`class : Subpage`） |
| 3 | 创建 PageModel | `PageModels/InstanceDependencyGraphPageModel.cs`（继承 `InstancePageModelBase`） |
| 4 | 激活已有菜单项 | `Pages/InstanceSetupPage.axaml:703`：`IsEnabled="False"` → `Command="{Binding GotoDependencyGraphPageCommand}"` |
| 5 | 添加导航命令 | `PageModels/InstanceSetupPageModel.cs`：`navigationService.Navigate<InstancePage>(new(Basic.Key, typeof(InstanceDependencyGraphPage)))` |
| 6 | （可选）添加 sidebar 入口 | `PageModels/InstancePageModel.cs:~134` `PageEntries` |
| 7 | 构建图数据 | PageModel 的 `OnInitializeAsync`：`ProfileManager.GetImmutable(Basic.Key).Setup.Packages` → 遍历 → `Info.Version` as `InstancePackageVersionModel` → `.Dependencies` |

### 关键 API 速查

**依赖数据调用链**：
```
ProfileManager.GetImmutable(Basic.Key).Setup.Packages   // IEnumerable<Profile.Rice.Entry>
  → InstancePackageModel.Info.Version                    // InstancePackageVersionModelBase
  → (as InstancePackageVersionModel).Dependencies        // IReadOnlyList<Dependency>
```

**Dependency record**（`submodules/Trident.Net/.../Dependency.cs`）：
```csharp
public record Dependency(
    string Label,       // 仓库标签（modrinth / curseforge）
    string? Namespace,
    string ProjectId,   // 唯一标识，适合做节点 key
    string? VersionId,
    bool IsRequired
);
```

**导航**（利用 `InstancePageModel.CompositeParameter`，已是现成机制）：
```csharp
navigationService.Navigate<InstancePage>(
    new InstancePageModel.CompositeParameter(Basic.Key, typeof(InstanceDependencyGraphPage)));
```

**DI 注册**：无需手动。`SimpleViewActivator` 按命名约定自动配对 `XxxPage` ↔ `XxxPageModel`（正则 `.Pages.` → `.PageModels.`，末尾 `Page` → `PageModel`）。

### 项目约定提醒

- **一个 .cs 文件一个类型**（AGENTS.md 硬规则）
- 命名空间：Page → `Polymerium.Avalonia.Pages`，PageModel → `Polymerium.Avalonia.PageModels`
- 本地化：POC 阶段可硬编码中文，稳定后再抽到 `Resources.resx` + `Resources.zh-hans.resx` + `Resources.Designer.cs`
- 菜单项文本 `InstanceSetupPage_DependencyGraphMenuText` 已存在（resx + Designer 都有），无需新增

---

## POLY-87 标签图的后续规划

POC（POLY-86）跑通后，POLY-87 复用同一 `GraphPanel` 控件：

- 图类型：二分图（标签节点 ↔ 包节点，边表示归属）
- 数据源：`Profile.Rice.Entry.Tags`（用户自定义）+ `Project.Tags`（仓库分类）
- 布局：力导向（MSAGL `Ranking` 或 `MDS`）比分层更适合多对多关系
- 入口：需新增（POLY-86 有现成禁用菜单项，POLY-87 没有）

---

## 进度日志

- **2026-06-19** 调研完成，确认两个 issue 都可实现；用户拍板路线 A + 先做 POLY-86
- **2026-06-19** 核实 License（MIT）+ 起 POC 分支 `feature/POLY-86-dependency-graph`
- **2026-06-19** 加包 `AvaloniaGraphControl 0.7.0`，NuGet 解析通过，全量 build 通过
- **2026-06-19** explorer 给出新增子页面的精确落地指南；待写 POC 代码
- **2026-06-19** researcher 调研 AvaloniaGraphControl 0.7.0 API：根元素 `agc:GraphPanel`、`Graph.Edges`、节点按引用比较、`DataTemplates` 按类型匹配、`LayoutMethod=SugiyamaScheme`、包不内置缩放需外层 `PanAndZoom.ZoomBorder`
- **2026-06-19** POC 阶段 1 完成：新增 `Models/DependencyGraphNode.cs`、`PageModels/InstanceDependencyGraphPageModel.cs`（硬编码 Minecraft→Fabric→mods DAG）、`Pages/InstanceDependencyGraphPage.axaml(.cs)`（ScrollViewer + GraphPanel + SugiyamaScheme + 主题色节点），激活 `InstanceSetupPage.axaml` 依赖图菜单项，`InstanceSetupPageModel` 加 `GotoDependencyGraphCommand`。全量 build 通过（0 Error，7.99s）。待人眼验证运行时渲染
- **2026-06-19** 边路由调研：用户要「横平竖直 + 合并同结构线条」。AvaloniaGraphControl 0.7.0 零暴露 EdgeRoutingSettings、零扩展点；MSAGL 无梳齿状母线合并能力（SplineBundling 只是样条束），正交路由在 Sugiyama 下初始布局无效（Issue #48）。结论：横平竖直需 fork 改 2-10 行，母线合并需自写算法 200-1000 行。用户决定暂搁置边路由，优先缩放
- **2026-06-19** POC 阶段 2 完成：加包 `SmoothScroll.Avalonia 12.0.0.12`（MIT，Composition 层 InteractionTracker 缩放，非 RenderTransform），App.axaml 注册 `ScrollViewDefaultTheme` 全局主题，页面 ScrollViewer→`smoothScroll:ScrollView`（IsZoomEnabled=True，滚轮直接缩放，拖拽平移，pinch，隐藏滚动条）。全量 build 通过（0 Error，13.6s）。待人眼验证缩放交互
- **2026-06-19** 用户反馈两个问题 + 一个产品决策：（1）拖拽总回到中心；（2）Ctrl+滚轮不缩放；（3）应做成 modal 而非页面。读 SmoothScroll 源码确认：（1）根因是可滚动区域为零（content≤viewport 时 MinPosition=MaxPosition=0，拖拽靠弹性错觉，松手 clamp 回原位），无配置项可关；（2）库根本不检查修饰键，IsZoomEnabled 时直接滚轮就缩放，不支持 Ctrl+滚轮模式。用户拍板：Modal 用 Stretch 接近全屏，缩放交互先改完 modal 再观察
- **2026-06-19** POC 阶段 3 完成：从 Subpage 改为 Modal。新建 `Modals/InstanceDependencyGraphModal.axaml(.cs)`（husk:Modal + Stretch + 底部关闭按钮 `Dialog_DismissButtonText` + smoothScroll:ScrollView + GraphPanel），Graph 构建逻辑（硬编码）从 PageModel 搬到 Modal 自身，走实例模式 `$parent[app:InstanceDependencyGraphModal].DependencyGraph` 绑定。`InstanceSetupPageModel.GotoDependencyGraph` 改为 `overlayService.PopModal(new InstanceDependencyGraphModal())`。删除旧 `InstanceDependencyGraphPage` + `InstanceDependencyGraphPageModel` + 用户手动加的 PageEntries 侧边栏入口。保留 `Models/DependencyGraphNode.cs`。全量 build 通过（0 Error，10s）。待人眼验证 modal 弹出 + 缩放交互在固定容器下的表现
- **2026-06-19** 用户实测 SmoothScroll 仍有问题：滚轮不滚动、拖拽总是回到原点看不到右半部分。决定弃用 SmoothScroll，自研 ZoomView 控件。用户拍板交互：直接滚轮缩放 + 中键拖拽平移 + 无限平移
- **2026-06-19** POC 阶段 4 完成：自研 `Controls/ZoomView.cs`（继承 ContentControl，RenderTransform + 单 Matrix，以鼠标为锚点的滚轮缩放公式 `M_new = ScaleAt(s,s,cx,cy) * M`，中键拖拽平移用视口坐标 delta，MinZoom/MaxZoom/ZoomSpeed 可配，Reset() 方法）。架构参考 PanAndZoom 的 ZoomBorder（不用 ScrollViewer/InteractionTracker，避开 SmoothScroll 的坑）。Modal 从 `smoothScroll:ScrollView` 换为 `app:ZoomView`。清理 SmoothScroll 全部引用：csproj 移除包、App.axaml 移除命名空间+两个主题（ScrollViewDefaultTheme 和用户手动加的 ScrollViewerSmoothTheme）、Modal 移除命名空间+控件。全量 build 通过（0 Error，19.96s，SmoothScroll 已从 restore 消失）。待人眼验证 ZoomView 缩放/拖拽手感
