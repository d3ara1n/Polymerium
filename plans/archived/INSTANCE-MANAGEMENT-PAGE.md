# INSTANCE-MANAGEMENT-PAGE

> 制定日期：2026-06-30
> 定位：把实例的"管理"职责从主界面侧边栏剥离到独立的 InstancesPage——主界面实例列表当前同时承载浏览 / 入口 / 状态 / 增删同步四职，过重且阻塞了排序、分组、批量操作等管理能力的扩展
> 当前状态：已实施（Phase A / B / C 落地，构建通过）。后半（贴标 UI / facet 筛选聚合 / 批量操作 / 视觉 polish）仍留白，下次计划续写
> 关联 Issue：[POLY-23](https://d3ara1n.atlassian.net/browse/POLY-23)

计划主角是独立的 InstancesPage（管理），主界面侧边栏只保留 Pinned / Active / Recent 三类轻量瞭望入口（见"三类来源定义"）。

## 背景与动机

主界面侧边栏的实例区（`MainWindow.axaml` Row 5–6）当前是"全量实例 + 搜索框 + 每条带运行状态"的复合列表，同时承担完整浏览、快速入口、状态展示、增删同步四职，导致两个问题：

1. **职责过载**：多数日常场景只需回到常用或正在运行的实例，却被全量列表与搜索框包围。
2. **管理能力缺失**：实例多了之后无法排序、分组、批量操作——"把实例当对象管理"没有落点。

POLY-23 写得早（编号小），其后为它扫清障碍的任务编号已到 99。障碍现状（均已落地，不再是前置依赖）：

- `InstanceStateAggregator` 已建成（`Services/InstanceStateAggregator.cs`），`MainWindowContext.SubscribeState` 已迁移到订阅其 `StateChangeStream`，不再直接挂 `InstanceManager` 四事件。
- `MainWindow.Instance` 静态属性已消除，外观状态抽出为 `ThemeService`。
- POLY-99（macOS 关窗不退出 + Dock 重开）已落地：`App.axaml.cs` 设 `OnExplicitShutdown`、挂 `IActivatableLifetime.Activated`、`OnActivated` 重建窗口。`MainWindowContext.OnDeinitialize` 反订阅完整（ProfileManager 三事件 `-=` + `_disposables.Dispose()` 清 RX 订阅），窗口重建无泄漏。

因此原设想里"把 MainWindowContext 实例管理搬运到 InstanceService 以解锁 POLY-99"的动机**已失效**——本计划不包含该搬运。

## 目标

- 主界面侧边栏实例区改为只显示 **Pinned / Active / Recent** 三类实例的合并视图，单列 tier 排序，无分区。
- 去掉主界面搜索框（搜索归 InstancesPage）。
- 新增 Pinned 能力：用户可 pin/unpin 实例，持久化。
- 新建 `InstancesPage`，承接被移出的全量浏览 + 基础排序 + 点击跳转实例页 + 右键菜单（导出 / 文件夹 / 设置 / 属性）。
- Accounts 按钮移位至 Settings 旁，原位置改为 InstancesPage 入口。

## 非目标 / 不做的事

- **不做 MainWindowContext → InstanceService 的实例管理搬运**（POLY-99 已不依赖）。
- **不做自定义分组规则 / 分组编辑 UI / 多维筛选 / 批量操作**——留白给下次计划。
- **不做"最近添加"的持久化**——Recent 纯进程内，关机即忘。
- 主界面实例区**不画分区分隔线**（空间成本过高）。
- InstancesPage **不显示运行状态**——状态归主界面，此页是纯管理视图。

## 核心设计

### 三类来源定义

| 来源 | 含义 | 数据源 | 生命周期 | 容量 |
|---|---|---|---|---|
| Active | `State != Idle` 的实例 | `InstanceEntryModel.State`（已有，由 Aggregator 写入） | 运行时随状态进出 | 通常 0–2 |
| Pinned | 用户主动 pin | `PersistenceService`（复用 `WidgetLocalSection` 机制） | 持久化 | 用户决定 |
| Recent | 本次启动内新建 / 导入 | 订阅 `ProfileManager.ProfileAdded`，进程内 FIFO | 关机即忘 | cap 3 |

### 显示规则

同一实例可能同时满足多种（如刚装且正在 Deploy）。**按 key 去重，取最高优先级 tier**：

```
Tier 0  Active   (State != Idle)           ← 永远最前，任何跑起来的实例都进
Tier 1  Pinned   (Idle 且已 pin)
Tier 2  Recent   (Idle 未 pin 本会话新建)
都不满足 → 不进主列表（去 InstancesPage 找）
```

### 布局

单列扁平排序，不画分隔线。tier 自然分层，首屏永远是最重要的几条；视口可滚动。

```
主界面侧边栏实例区（可滚动）
┌──────────────────────────────────────────┐
│ ▸ 🟢 My Main     Running                 │ ← Active（淡底色强化）
│ ▸ 🔵 Skyblock    Installing 45% ▕▕▕▕▕    │ ← Active
│ 📌  Forge 1.20    Idle          2h ago   │ ← Pinned
│ 📌  Fabric Test   Idle          yesterday │ ← Pinned
│ ✨  New Import    Idle                    │ ← Recent
└──────────────────────────────────────────┘
```

视觉约定：

- Active 行加淡背景色，强化外围视野感知。
- Pinned 行带 📌 角标。
- Recent 行带 ✨ 或 "new" 标记。
- Pinned / Recent 的 Idle 行可保留 `LastPlayedAt` 弱显示，与现状一致。

### 数据管线：钩子内判断，运行时不做全量过滤

`_entries`（`MainWindowContext.cs:55` 的 `SourceCache<InstanceEntryModel, string>`）语义从"全量 profile"收窄为"P ∪ A ∪ R 应显示集"。在现有的增删 / 状态钩子里直接判断成员资格，pipeline 退化成只剩 `.Sort(tierComparer).Bind()`：

| 钩子（行号为现状） | 现有职责 | 新增成员资格职责 |
|---|---|---|
| `SubscribeProfileList` (`:551`) 启动遍历 | 全量加入 `_entries` | **改为只加 Pinned**（启动时 R 为空；非 P 的历史 Idle 实例不进） |
| `OnProfileAdded` (`:571`) | 建模型 | 追加进 R（FIFO cap 3，挤出最早的）；若已 P 则不动 |
| `HandleSnapshotUpdate` (`:650`) → 非 Idle | 写 State / Progress | **确保在 `_entries`**——含从 InstancesPage 启动的非 P 非 R 实例 |
| `HandleSnapshotRemove` (`:676`) → Idle | 清 State | 若**非 P 且非 R**，移出 `_entries` |
| `OnProfileRemoved` (`:614`) | 从 `_entries` 移除 | 同步清 P / R，unpin 持久化（防 stale pinned key） |
| 新增 `Pin` / `Unpin` 命令 | — | 改 P 集 + 持久化 + 调整 `_entries` 成员 |

`tierComparer` 读 `model.State`（A 判据）+ 新增 `IsPinned`（P 判据）+ Recent 序号（R 判据）。

边界用例：

| 场景 | 行为 |
|---|---|
| Pinned 实例开始运行 | 升 Tier 0，保留 📌 |
| Recent 实例被 pin | 升 Tier 1，丢 ✨ |
| 未 pin 非 recent 的实例从 InstancesPage 启动 | 进 Tier 0（状态必须可见）；回 Idle 后移出 |
| 重启应用 | Recent 清空；未 pin 的 Idle 实例从主列表消失 |
| 删除实例 | 从 P / R 清理 + unpin 持久化 |

### Pinned 持久化与共享

Pinned key 集合复用 `PersistenceService` 的 `WidgetLocalSection` 机制（`GetWidgetLocalData<T>(key, widgetId, indicator)` `:370` / `SetWidgetLocalData<T>(...)` `:379`），用固定的 `(widgetId, indicator)` 存 `string[]`。不新建数据表，不引入 schema 变更。

> NOTE: Pinned 集合归属从原设计「MainWindowContext 内部 HashSet」调整为 **InstanceService 持有的可观察状态**。pin / unpin 不仅改主界面 `_entries` 成员资格，还要从 InstancesPage 右键触发——集合必须跨 VM 共享。InstanceService 持有它并暴露变更流，MainWindowContext 订阅构建 `_entries` 的 P 成员，InstancesPageModel 订阅刷新卡片 📌 角标；任一处改 pin，另一边自动刷新。

### InstancesPage 设计（Phase C）

整页新建，定位是**分类管理 + 导航**，纯展示、无运行状态。承载后半（分组 / 筛选 / 批量）的生长面，故用卡片网格而非列表——列表行等于把主界面侧边栏小列表原样搬个家，抽独立页面毫无意义。

#### 卡片控件：新建 InstanceCard，不复用 InstanceEntryButton

`InstanceEntryButton` 的 ControlTheme 绑定 `State` / `Progress` / `IsPending`，并用 `^[State=Installing]` / `=Updating` / `=Deploying` / `=Running` 四组选择器切换状态 Tag 与进度条可见性——它一身兼展示 + 状态瞭望 + 动作入口，正是主界面侧边栏"职责过重、无法扩展管理能力"的根因。复用等于把压垮它的东西原样搬过去，分类 / 筛选 / 批量仍无处生长。

新建 `Controls/InstanceCard.axaml`（ControlTheme）+ `Controls/InstanceCard.cs`，纯展示 + 跳转，**零按钮**。绑展示字段：

| 绑定 | 来源 | 样式 |
|---|---|---|
| 封面 | `Basic.Thumbnail` | **小正方形**（保持 icon 原貌不拉伸）+ 周围主题色渐变兜底。icon 是唯一位图素材且质量参差，不做横向 banner（拉伸变形） |
| 钉住角标 | `IsPinned` | 已钉住才显 📌，左上角 |
| 名称 | `Basic.Name` | 主角，大字粗体 |
| 来源 | `Basic.SourceLabel` | 次要色单行。**不带 `#Key`**（技术噪音）、不带 "from"（i18n 不好做） |
| 固有属性 | `Basic.LoaderLabel` / `Basic.Version` | **淡 inline**（`┄ Fabric · 1.20.1`），与用户标签严格区分 |
| 用户标签 | `Tags` | hash 自动分配主题色的色点 + 名，溢出收 `+N`；无标签时整段隐去 |
| 上次游玩 | `LastPlayedAt` | 最弱，沉底 |

固有属性（系统读出的技术身份）和用户标签（主观贴的分类）**必须不同样式**——前者淡 inline，后者色点药丸——否则用户分不清「哪些是我标的」。

整卡点击 = `Navigate<InstancePage>(key)`；hover = accent 边框 + 微缩放（沿用 `AccountEntryButton`）；**卡片上无任何按钮**，动作全进右键菜单（见下）。

#### 卡片模型：新建 InstanceCardModel，不走 InstanceEntryModel

`InstanceEntryModel` 是主界面专属——带 `State` / `Progress` / `IsPending` + Aggregator 订阅钩子。管理页不掺和状态，故新建 `Models/InstanceCardModel.cs`：

- 复用 `InstanceBasicModel` 作 `Basic`（含 Key / Name / Version / LoaderLabel / SourceLabel / Thumbnail + `UpdateIcon()`）
- 加 `LastPlayedAtRaw`（`ObservableProperty` + `[NotifyPropertyChangedFor(nameof(LastPlayedAt))]`，Humanize 输出，照 `InstanceEntryModel` 同款写法）
- 加 `IsPinned`（`ObservableProperty`，从 InstanceService 的 Pinned 可观察集合派生）
- 加 `Tags`（`ObservableCollection<string>`，从 PersistenceService 按 key 读取）
- 不含 State / Progress / IsPending

#### 布局：卡片网格全宽，筛选走 flyout

`husk:Page` + `Header`（标题 + 右侧按钮组：排序 ▾ / 筛选 / 新建实例）+ 搜索框（激活的筛选项以可移除药丸回显在搜索栏旁）+ 空态 + `ItemsControl` / `WrapPanel` + `InstanceCard` ItemTemplate + `ContextFlyout`。

筛选不常驻——点 Header 的「筛选」按钮弹 `Flyout`，内含固有 facet（加载器 / 版本 / 来源）+ 用户标签多选；排序按钮挨着，下拉选名称 / 上次游玩 + 升降序。这样卡片网格保持全宽，不被常驻筛选条或分组侧栏吃横向空间。骨架仍沿用 `AccountsPage.axaml`。

#### 数据源：直连 ProfileManager 全集

不走主界面的 `_entries`（那是 P ∪ A ∪ R 收窄集）。`InstancesPageModel.OnInitializeAsync` 遍历 `profileManager.Profiles`，对每个 `(key, item)` 构建 `InstanceCardModel`（`LastPlayedAtRaw` 取 `persistenceService.GetLastActivity(key)?.End`），订阅 `ProfileAdded` / `Updated` / `Removed` 增删卡片模型；`OnDeinitializeAsync` 反订阅。

#### 排序 + 搜索 + 筛选

DynamicData `SourceCache<InstanceCardModel, string>` + `.Filter(composite).SortAndBind(SortExpressionComparer)` 绑到 `ItemsControl`。filter 是搜索文本（名称包含）与 flyout 选中的 facet / 标签的复合谓词。最小版两个排序键（名称、上次游玩）。搜索框常驻顶栏，facet / 标签筛选走 flyout。

#### 标签（用户自定义）

用户标签是实例的**主观用途分类**（联机 / 测试 / 已弃坑……），补充固有 facet（加载器 / 版本 / 来源）覆盖不了的维度。

- **持久化**：`PersistenceService` 按 Profile key 存 `string[]`（走与 Pinned 同类的轻量存储，**不碰 Profile 本身**——标签是 Instance 的管理属性，不是 Profile 的技术属性）。全局标签列表不单独存，运行时扫所有实例的 tags 去重聚合，增删自动反映。
- **卡片展示**：色点 + 名，色点颜色按标签名 hash 取主题色板（零用户成本有视觉区分）；溢出收 `+N`。
- **筛选**：在筛选 flyout 的「我的标签」区多选，与 facet 复合成 filter。
- **贴标入口**：右键菜单「编辑标签」项或实例属性页——具体交互属后半，最小版只做展示 + 筛选消费。

> 标签的展示与筛选进 Phase C（卡片和 flyout 本来就要画）；贴标 / 管理标签的 UI 进后半。

#### 右键菜单与命令分层

三处实例右键（主界面侧边栏 / InstancesPage / InstancePage）内容基本一致——实例的动作需求到处都一样，区别只在载体不在动作集。有了 InstanceService 聚合动作逻辑，三处各自持 `[RelayCommand]` 一行转发，代码不重复，也不存在「迁移」——每个 VM 注入 InstanceService 即可。

实例右键标准集（主界面当前右键 7 项 `MainWindow.axaml:419-455` + 钉住）：启动(Play) / 部署(Deploy) / — / 导出(Export) / 文件夹(OpenFolder) / — / 设置(GotoSetup) / 属性(GotoProperties) / — / 钉住(Pin/Unpin)。

动作逻辑归属：

| 动作 | 逻辑归属 | 说明 |
|---|---|---|
| 启动 / 部署 | InstanceService（**已有** `DeployAndLaunch` / `Deploy`） | 原本就在 InstanceService |
| 导出 / 打开文件夹 / 设置 / 属性 | InstanceService（**下沉**） | 当前在 MainWindowContext；下沉后 InstancePage.axaml:248 改绑 InstancePageModel 自身命令（修 `$parent` 反模式） |
| 钉住 / 取消钉住 | InstanceService（**新增**） | 改 Pinned 可观察集合 + 持久化；主界面 / 管理页订阅自动刷新 |

InstanceService 新增依赖：ExporterAgent、OverlayService、NotificationService、NavigationService。各 VM 的 Command 方法体 = `_instanceService.Xxx(key)`。

> NOTE: 启动在右键里只是动作项，不要求卡片承担状态显示——卡片仍纯展示无运行状态，启动的状态反馈归主界面 Active 区和实例页。

各 ViewModel 的 Command（全部保留 / 新增，转发 InstanceService）：

| ViewModel | 持有 Command | 转发 |
|---|---|---|
| `MainWindowContext` | Play / Deploy / Export / OpenFolder / GotoSetup / GotoProperties / Pin / Unpin / ViewInstance（**全部保留**，方法体改转发 InstanceService） | → InstanceService |
| `InstancesPageModel`（新建） | 同上全套 | → InstanceService |
| `InstancePageModel` | 整理右键为符合实例场景的全套（导出等），转发 InstanceService；修 `:248` 的 `$parent` 反模式 | → InstanceService |

点击导航：`ViewInstance` → `Navigate<InstancePage>(key)`（`InstancePage` 外壳，非 `InstanceWorkspacePage` 子页；原计划笔误已修正）。

### 入口移位

`MainWindow.axaml` Row 8（`:508` 附近）：Accounts 按钮（Column 1–2）与 Settings 按钮（Column 0）位置调整——Accounts 移到 Settings 旁，原 Accounts 位置改为 InstancesPage 入口（`Navigate<InstancesPage>()`）。纯 XAML + 命令。

## 改动面

| 文件 | 改动 | 阶段 |
|---|---|---|
| `Models/InstanceEntryModel.cs` | 新增 `IsPinned`（ObservableProperty）+ Recent 序号字段 | A |
| `Services/PersistenceService.cs` | 新增 Pinned key 集合存取 + 标签按 key 存取（复用 `WidgetLocalSection`，`:370` / `:379` 附近） | A/C |
| `Services/InstanceService.cs` | 充实：新增 `ExportInstanceAsync` / `OpenFolder`（按 key）/ `GotoProperties` / `GotoSetup` / `Pin` / `Unpin` 编排 + **Pinned 可观察集合**（跨 VM 共享）；新增依赖 ExporterAgent / OverlayService / NotificationService / NavigationService。动作下沉前置于 Phase B（主界面命令转发依赖它） | A/B |
| `MainWindowContext.cs:55` | `_entries` 语义收窄；订阅 InstanceService 的 Pinned 可观察集合构建 P 成员 + Recent FIFO 列表 | A |
| `MainWindowContext.cs:551` `SubscribeProfileList` | 改为只加 Pinned | A |
| `MainWindowContext.cs:571` `OnProfileAdded` | 追加 R（cap 3） | A |
| `MainWindowContext.cs:614` `OnProfileRemoved` | 同步清 P / R + unpin 持久化 | A |
| `MainWindowContext.cs:628/650/676` 状态钩子 | 增加成员资格判断（Active 进、Idle 非 P 非 R 出） | A |
| `MainWindowContext.cs` filter pipeline（`:89`–`:97`） | 删除 `FilterText` + `BuildFilter`，pipeline 退化为 `.Sort(tierComparer).Bind()` | B |
| `MainWindowContext.cs` | Play / Deploy / Export / OpenFolder / GotoSetup / GotoProperties 命令方法体改转发 InstanceService；新增 Pin / Unpin 命令；ViewInstance 保留 | B/C |
| `MainWindow.axaml` Row 5（`:352` 附近） | 删除搜索框 `TextBox` | B |
| `MainWindow.axaml` Row 6 | 实例区右键菜单**保留全部**，追加 pin / unpin 项；命令改绑转发后的 MainWindowContext 命令 | B |
| `MainWindow.axaml` Row 8（`:508` 附近） | Accounts 移到 Settings 旁，原位改 Instances 入口（`Navigate<InstancesPage>()`） | C |
| `Models/InstanceCardModel.cs` | **新建**：Basic + LastPlayedAtRaw + IsPinned + Tags，无运行状态 | C |
| `Controls/InstanceCard.axaml(.cs)` | **新建**：纯展示卡片，零按钮；小正方形封面 + 渐变兜底 + 📌 角标 + 名称/来源/固有属性淡 inline/标签色点/上次游玩；整卡点击导航 | C |
| `Pages/InstancesPage.axaml(.cs)` | **新建**：全宽卡片网格 + Header（排序 ▾ / 筛选 flyout / 新建）+ 搜索框（激活筛选回显）+ 空态 + 右键菜单（7 项 + 钉住） | C |
| `PageModels/InstancesPageModel.cs` | **新建**：ProfileManager 全集 → InstanceCardModel；订阅增删 + 订阅 InstanceService Pinned 刷新角标；Filter（搜索+facet+标签）+ SortAndBind；Play / Deploy / Export / OpenFolder / GotoSetup / GotoProperties / Pin / Unpin / ViewInstance 转发 InstanceService | C |
| `Pages/InstancePage.axaml:248` | `$parent` ExportInstanceCommand → 绑定 InstancePageModel 自身命令（修反模式） | C |
| `PageModels/InstancePageModel.cs` | 整理右键为符合实例场景的全套（导出等），转发 InstanceService；修 `:248` `$parent` 反模式 | C |
| `Properties/Resources.resx` / `Resources.zh-hans.resx` / `Resources.Designer.cs` | 新增 Pinned / Recent / InstancesPage / InstanceCard / 标签相关文案（三文件同步） | B/C |

## 阶段

| 阶段 | 内容 | 状态 |
|---|---|---|
| Phase A | 数据层：`IsPinned` / `RecentOrder` 字段、Pinned / 标签持久化、钩子重构（`_entries` 收窄为 P ∪ A ∪ R）。**实施调整**：Pinned 集合归属从原设计的 MainWindowContext 内部 HashSet 改为 InstanceService 持有的可观察 `SourceCache`（跨 VM 共享，因 pin 入口在多处） | ✅ 已完成 |
| Phase B | 主界面 UI：tier 排序 + 去搜索框 + pin / unpin 交互。**实施调整**：右键菜单保留全部（原设计「取消仅留 pin/unpin」改为「保留全套 + pin/unpin」，因 InstanceService 聚合后无需迁移）；视觉强化只做了 pinned 角标，Active 淡底色 / Recent ✨ 延后（InstanceEntryButton 已有状态 Tag，非阻塞） | ✅ 已完成 |
| Phase C | InstancesPage 全套（InstanceCard + InstanceCardModel + InstancesPage(.axaml/.cs) + InstancesPageModel）+ InstanceService 充实（导出 / 打开 / 跳转 + Pin/Unpin，Command 留 VM 转发）+ InstancePage.axaml:248 反模式修正 + 入口移位。**实施调整**：筛选 Flyout 最小占位（facet 聚合延后至后半）；标签 +N 截断延后（现 wrap 布局） | ✅ 已完成 |
| 后半 | 贴标 / 管理标签 UI + facet 筛选聚合进 Flyout + 批量操作（多选 + 批量导出 / 删除 / 部署）+ 自定义分组（视需要）+ 视觉 polish（Active 淡底色 / Recent ✨ / tags +N 截断） | 留白，下次计划 |

## 验收标准

| 场景 | 期望 |
|---|---|
| 全新用户（无实例） | 主界面实例区空态 |
| 装第一个实例 | 进入 Recent，主界面可见（即使未 pin） |
| pin 一个实例 | 升 Tier 1，带 📌；重启后仍在主界面（Pinned 持久化生效） |
| 启动一个未 pin 的实例 | 升 Tier 0 可见；停止后移出主界面 |
| 实例超过 4 个 | 首屏只显 tier 最高的几条，可滚动 |
| 删除已 pin 实例 | 从主界面消失，pinned 持久化中清除（无 stale key） |
| 进 InstancesPage | 看到全量卡片网格（无状态徽章 / 进度条），可排序，搜索可过滤，点击进实例页，右键有导出 / 文件夹 / 设置 / 属性 |
| 实例页内导出 | `InstancePage.axaml:248` 改绑 InstancePageModel 自身后导出仍正常（验证 `$parent` 反模式修正） |
| Accounts 入口 | 在 Settings 旁，原位置变成 Instances 入口 |

## 备选方案备案

### 载体：为何选 Page 而非 Sidebar / Toast

issue 正文里 Page / Toast / Sidebar / Drawer 术语混用，长期未定。最终选 Page：

- **Sidebar（边缘滑入浮层）**：太窄只能纵列，与改造前的主界面列表无本质区别。"一眼看到全部实例"是全功能管理的首要要求，纵列窄边栏满足不了，效率无提升，动机不足。
- **Toast（本项目语义）**：瞬态、自动消失，不适合持续浏览 + 搜索输入的列表容器。
- **Page（全屏导航）**：空间最大，适合全功能管理；点击实例跳转实例页的层级自然。issue 当年把 Page 否掉的理由（具体未载）在当前设计下已不成立。

### 布局：为何单列扁平而非分区

- 状态可见性是每条条目自带（`InstanceEntryButton` 已有徽章 + 进度条），分区不增加状态可见度，只影响排序——而排序靠 tier 比较器即可白拿。
- 分隔线在 3–4 条视口里吃掉 25–50% 空间，代价致命。
- 扁平 tier 排序保证首屏质量最高，溢出可滚。

主界面是否显示运行状态，曾出现两种相反表述（issue 原文"不显示状态" vs 后续"状态需要常驻一眼看到"）。最终以最新意图为准：**主界面必须显示状态**，状态是常驻瞭望的核心价值；不显示状态的是 InstancesPage。

### Recent：为何不持久化

"最近添加"反映的是"当前会话的操作痕迹"，重启后语境已变，持久化反而引入"何时清理"的心智负担。进程内 FIFO + cap 3 + 关机即忘最简。

### Pinned：为何复用 WidgetLocalSection 而非新表

`WidgetLocalSection` 已是"按实例 key 分区的 JSON blob"通用机制，Pinned key 集合是其自然用例。新建表引入 schema 变更与迁移成本，无收益。

### 搬运：为何不做 MainWindowContext → InstanceService

该搬运的原动机是"解锁 POLY-99 的窗口重建"。POLY-99 已落地且 `OnDeinitialize` 反订阅完整、无重建泄漏，动机失效。搬运现为纯代码整洁，无紧迫性，留待日后整体重构时一并考虑。

### 为何不引入 SourceCache 冗余 P / A 表

Active 判据是 `entry.State != Idle`（条目自身属性），无需独立 key 集。Pinned 是唯一需要独立集合的来源，HashSet / 小 SourceCache 足矣，不与 `_entries` 重复建模。

### 钩子内判断 vs 运行时全量过滤

曾在" `_entries` 仍存全量 + pipeline 运行时 filter"与"`_entries` 收窄为应显示集 + 钩子内判断"之间取舍。选后者：`_entries` 始终精确等于视图，pipeline 退化只剩排序，语义干净；现有两组钩子（Profile 增删、State 变更）本就在跑，每处只加一句成员资格判断，职责内聚。

### 卡片控件：为何不复用 InstanceEntryButton

`InstanceEntryButton` 的 ControlTheme 绑 `State` / `Progress` / `IsPending`，并通过 `^[State=Installing]` / `=Updating` / `=Deploying` / `=Running` 四组选择器切换状态 Tag 与进度条可见性——它一身兼展示 + 状态瞭望 + 动作入口，正是主界面侧边栏"职责过重、无法扩展管理能力"的根因。本计划的全部动机就是把这个过载的列表拆出去做独立管理页；若管理页继续复用它，等于把压垮它的东西原样搬过去，分类 / 筛选 / 批量等后半能力仍然无处生长。故新建无状态的 `InstanceCard`，只绑 `Basic` 展示字段，状态彻底留给主界面。

### 命令分层：为何下沉 InstanceService 而非复制或爬 shell

`ExportInstanceAsync`（154 行 UI 编排）当前被主界面右键与 `InstancePage.axaml:248` 共用，后者通过 `$parent[app:MainWindow].MainWindowContext.ExportInstanceCommand` 爬到 shell 找命令——这本身就是反模式。三选一：

- 复制到各 ViewModel：154 行逻辑复制三份，日后漂移。
- 保留爬 shell：InstancePage 继续依赖 MainWindowContext，反模式固化。
- **下沉 InstanceService**：`InstanceService` 当前仅 `DeployAndLaunch` / `Deploy`，是薄封装、功能薄弱，正好充实为"实例相关编排"的归集处。逻辑进 service，各 ViewModel 注入它、持有 `[RelayCommand]` 薄转发（`_instanceService.Xxx(key)`）。

选第三条：业务 / IO / 导航编排归 InstanceService，Command 留 ViewModel——MVVM 的正解分层，且顺手修掉 InstancePage 的 `$parent` 反模式。

> NOTE: 这里的"动作编排下沉"与非目标里那条"不做 MainWindowContext → InstanceService 的实例管理搬运"不冲突——后者指 `_entries` 状态 / 列表归属（POLY-99 动机已失效，不动），前者指导出 / 打开 / 导航等无状态动作的归集。两者是不同层面的东西。

`OpenFolder` 已有多处分头实现（MainWindowContext 按 key、InstancePageModel、`InternalCommands` 按 path）；主界面那套随右键取消删除，InstancesPage 复用 InstanceService 的按 key 版本（`OpenFolder` 的按 path 版本保留给文件页等按 path 场景）。

### 卡片交互：为何零按钮，启动进右键而非卡片

曾在「卡上显式按钮（启动 / 跳转 / 更多）」与「整卡点击 + 零按钮」之间取舍。选后者：

- 实例卡片动作多（启动 / 部署 / 导出 / 文件夹 / 设置 / 属性 / 钉住），全做成按钮会把卡片挤满、喧宾夺主，违背「纯展示 + 留白」。
- 整卡点击做最高频的「进实例页」，其余动作收进右键菜单——桌面用户右键、触屏长按，不占卡片视觉。
- 启动特意不进卡片显式按钮：启动是强状态动作（运行中变停止），但管理页定位是无运行状态。放卡片上会逼卡片承担状态显示，破坏定位。启动留在右键菜单里作为动作项——点了就启动，状态反馈归主界面 Active 区和实例页，卡片始终无状态。

封面用小正方形 icon + 渐变兜底而非横向 banner：icon 是 instance 唯一位图素材、且来自整合包的图标质量参差，正方形拉成横向 banner 必变形。小尺寸保持原貌 + 渐变托底，质量差也耐看。
