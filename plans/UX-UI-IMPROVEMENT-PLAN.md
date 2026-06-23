# Polymerium.Avalonia UX/UI 改进计划

> 评估范围：从用户角度审视界面设计、布局、功能分布、交互动线与一致性。不涉及代码质量与 bug。
> 评估日期：2026-06-23
> 评估方法：通读主窗口、Landing、实例详情容器及 9 个子页面、设置页、创建实例/首次引导流程，以及 20+ 个对话框/模态/侧栏/控件。

## 进度跟踪

> 最近更新：2026-06-23

| 编号 | 问题 | 状态 |
|------|------|------|
| P1-5 | 日志级别过滤器加文字标签 | ✅ 已完成 |
| P2-8 | 侧边栏 Toolbar 区重构（通知移左 + 更多菜单） | ✅ 已完成 |
| P2-10 | 本地化缺口修补（tooltip / 硬编码中文） | ✅ 已完成（Random Play 暂保留 🤔） |
| P2-11 | Super Power 重命名为「高级与实验性」 | ✅ 已完成 |
| P2-12 | 设置页导航锚点 | ⏳ 待定（详见该节方案） |
| 其余 | P0-1/2/3、P1-4/6、P2-7/9、P3 | 未开始 |

## 总体判断

这是一个视觉精致度相当高、动效和状态处理用心的应用——毛玻璃侧栏、启动按钮缩放动效、`InstancePackageButton` 的四态处理、OOBE/更新弹窗的状态机都做得很专业。

问题不在"好不好看"，而在：

- **信息架构对新手不友好**
- **核心任务动线绕**
- **专业功能暴露过度**
- **一致性有缺口**

核心矛盾：产品定位在"给硬核玩家/开发者用的专业工具"和"给普通玩家用的启动器"之间摇摆。

---

## 🔴 P0 — 影响新手能否上手的根本问题

### 1. 实例内部是"纯图标盲导航"，可发现性极低

**位置**：`src/Polymerium.Avalonia/Pages/InstancePage.axaml`

进入实例后，左侧是一条 64px 的图标导航栏，9 个子页面（首页 / Mod 管理 / 活动 / 控制台 / 文件 / 属性 / 存储 / 工作区 / 小部件）全部只用图标表示，文字标签只出现在 hover tooltip 里。底部还有 4 个工具按钮（导入/导出/快照/打开文件夹）。

**影响**：

- 桌面端 tooltip 需要悬停等待，笔记本/触摸板体验差，触摸设备完全无法触发
- 新玩家根本猜不出哪个图标是"Mod 管理"——而这恰恰是进实例后最常用的功能

**改进方向**：

- 改成"图标+文字"的常规侧栏（可折叠），或至少在选中态下方常驻当前页面标题
- 底部工具按钮的 tooltip 目前是硬编码英文（`Import asset from file` 等），需本地化并考虑挪到更明显的位置

### 2. 创建实例流程漏了最关键的一步

**位置**：`src/Polymerium.Avalonia/Pages/NewInstancePage.axaml`

`NewInstancePage`（新建实例）里没有加载器（Forge/Fabric/Quilt）选择，只能填名字 + 选游戏版本，创建后得再进 Setup 里点 `EditLoader`。但"装 Mod"是 90% 玩家创建实例的唯一动机。

另外版本选择的入口是一个藏在输入框右侧的小 `List` 图标按钮，还套了 SkeletonContainer，不醒目。

**改进方向**：

- 把"游戏版本 + 加载器"做成创建流程里的显式步骤（卡片或分段选择），而不是事后补
- 提升版本选择入口的视觉权重

### 3. "怎么启动游戏"不直观

启动主操作藏在：侧栏点实例 → 进 `InstanceHome` → 右下 `Launch Pad` 的大火箭按钮。

侧栏实例项虽然右键有 Play，但右键菜单是隐藏入口。Landing 的"继续游玩"只对最近玩过的实例有效。

**影响**：新用户首次创建实例后，很可能在实例列表前愣住"然后呢"。

**改进方向**：

- 实例列表项本身提供一级"启动" affordance（如悬停露出播放按钮）
- 创建成功后直接落地到 `InstanceHome` 的 Launch Pad

---

## 🟠 P1 — 功能密度过载，专业功能"裸露"给普通用户

### 4. Mod 管理页工具栏拥挤到令人窒息

**位置**：`src/Polymerium.Avalonia/Pages/InstanceSetupPage.axaml`

一个工具栏里同时塞了：

- 搜索框（还支持 `@Author #Summary !Id` 这种查询语法，但只写在 tooltip 里）
- 结果计数
- 刷新指示
- 规则计数 + 编辑
- 列表/网格切换
- SplitButton（批量更新 / 依赖图 / 导入列表 / 导出列表）

多维过滤（状态/来源/类型/标签）全藏在搜索框旁的 Filter 图标的 Flyout 里。

**影响**：

- 对普通玩家这是"专业工具"既视感，学习曲线陡
- 高级用户则要二次点击才能用到过滤

**改进方向**：

- 常用过滤（启用/禁用、类型）直接外露成分段控件
- 查询语法做成可展开的"高级搜索"提示而非纯 tooltip

### 5. 日志级别过滤器只有色块、没有文字

**位置**：`src/Polymerium.Avalonia/Pages/InstanceDashboardPage.axaml`

Info/Warn/Error 过滤是三个 ToggleButton，内容只有一个色块圆点，用户得猜"白=Info、黄=Warn、红=Error"。

**改进方向**：

- 加文字标签或至少首字母（I/W/E），并配 tooltip

### 6. 工程级功能直接暴露给终端玩家

**位置**：

- `src/Polymerium.Avalonia/Modals/ProfileRulesModal.axaml`（规则引擎，含 And/Or/Not/Purl 选择器）
- `src/Polymerium.Avalonia/Modals/InstanceDependencyGraphModal.axaml`（Sugiyama 有向图 + 0.1x–10x 缩放）

入口就摆在 Mod 管理工具栏里。普通 Minecraft 玩家看到"And/Or/Not 选择器"会完全懵。

**改进方向**：

- 这类功能收进"高级/开发者"模式（正好设置里已有 `Super Power` 开关，可以联动），默认隐藏

---

## 🟡 P2 — 一致性、文案、未完成留白

### 7. 全局导航按钮风格四种混用，层级混乱

**位置**：`src/Polymerium.Avalonia/MainWindow.axaml`

右侧导航同一区域里：

- Home = `OutlineButtonTheme`
- Marketplace = `Primary`
- Settings = `GhostButtonTheme`
- Accounts = `OutlineButtonTheme`
- 新建 = `OutlineButtonTheme`

视觉权重不一致，用户分不清主次。

**改进方向**：

- 主导航统一一种 theme
- 仅用 Primary 标记当前最想引导的动作（如 Marketplace 或"新建"其一）

### 8. 侧边栏职责过载 + 一个莫名其妙的 "Toolbar" 标题

**位置**：`src/Polymerium.Avalonia/MainWindow.axaml`

右侧栏同时是：主导航 + 更新/通知工具条 + 实例搜索 + 实例列表（主体）+ 底部设置/账户。更新/通知上方还有一个硬编码的英文 `Toolbar` 分组标题，语义不清且未本地化。

**改进方向**：

- 把"主导航"和"实例列表"在视觉上更明确地分区
- `Toolbar` 换成有意义的本地化标题或直接去掉

### 9. 空状态有 4 套不同实现，风格割裂

四种模式在不同页面混用：

- `EmptyContainer`（带 Icon 参数）
- `PlaceholderContainer`
- 手动堆 `SymbolIcon + TextBlock`
- `SkeletonContainer`

唯一做得到位的是 `InstancePackageButton`（加载/失败/禁用/正常四态完整）。

**改进方向**：

- 统一成一套空状态/加载/错误组件，全应用复用

### 10. 多处本地化缺口（用户可见的英文/裸中文）

| 位置 | 问题 |
|------|------|
| `InstancePage.axaml` 底部工具按钮 tooltip | 纯英文（`Import asset from file` 等） |
| `SnapshotManagementPage.axaml` 统计标签 | "包数量/文件数/总大小"硬编码中文 |
| `ProfileRuleSelectorsModal.axaml` | "添加新选择器"硬编码中文 |
| `LandingPage.axaml` Random Play | 用 🤔 emoji 当主图标，和全应用 FluentIcons 体系冲突 |

### 11. 未完成功能留下可见的"占位空洞"

| 位置 | 问题 |
|------|------|
| `LandingPage.axaml` Announcements 区域 | `IsVisible="False"` 硬编码隐藏 |
| `InstanceHomePage.axaml` Favorite 徽章 | 硬编码隐藏 |
| `InstanceActionCard.axaml` Undo 按钮 | `IsVisible="False"` 但仍占位 |
| `SettingsPage.axaml` Font | 一个禁用的 TextBox + `(Builtin)` 占位 |
| `SettingsPage.axaml` Super Power | "超能力"命名玩梗，用户不知道开启后果 |

### 12. 设置页是超长单列滚动，无导航锚点  ⏳ 待定

**位置**：`src/Polymerium.Avalonia/Pages/SettingsPage.axaml`

分组多达 7 个（高级与实验性/显示/Java/游戏默认/网络/维护/关于），找设置项只能滚动。根容器 `MaxWidth="1440"` 居中导致宽屏左右大量留白。

**改进方案（待实现）**：

1. **两栏布局**：根容器 `StackPanel` → `Grid ColumnDefinitions="220,*"`
   - 左栏 ~220px：分组导航列表（图标 + 标题，风格对齐 MainWindow 导航按钮）
   - 右栏：现有 7 个 `SettingsEntry` 原样塞进 `ScrollViewer`，每个加锚点（`x:Name`）
2. **联动高亮**（核心，需 code-behind 约 20 行）：
   - 点击左栏 → 右侧 `ScrollViewer` 滚动定位到对应分组（`BringIntoView`）
   - 右侧滚动时反向高亮当前可见分组（监听滚动事件计算）
3. **窄屏适配**：用 Avalonia 12 `ContainerQuery`（`Query="max-width:900"`）在容器变窄时设置左栏 `IsVisible="False"`，退回单列滚动
4. **主体宽度调整**：去掉/收窄 `MaxWidth="1440"`，让左导航占据原留白区，整体布局更紧凑美观

**暂不实现**：联动方案实现成本较高，先记录方案，后续单独迭代。

---

## 🟢 P3 — 反馈与边缘情况

- **异步图片无加载失败 fallback**：`AccountEntryButton`（皮肤立绘）、`InstanceEntryButton`（缩略图背景）、新闻封面加载失败时没有占位/错误态，可能呈现纯色块
- **`SnapshotManagementPage`**：搜索框无清空按钮；"还原(Primary) + 删除(Danger)"并列在同一行，易误删，建议删除走二次确认
- **`InstanceHomePage` Launch Pad**：未选账户仍可点启动，无账户时显示红色 "No account"，但启动按钮照样可见可点，应在此时禁用启动并直接引导去添加账户

---

## 跨界面一致性观察（来自子控件评估）

**做得好的**：

- 关闭按钮风格统一：`Button Classes="Small"` + `CornerRadius="FullCornerRadius"` + `SymbolIcon Symbol="Dismiss"`
- 主操作按钮语义统一：`Classes="Primary"`，危险操作 `Classes="Danger"`
- 间距体系有 margin 变量运作（`ModalContentMargin` / `SidebarContentMargin`）
- 卡片层级背景体系（`OverlayInteractiveBackgroundBrush` / `CardBackgroundBrush` / `LayerBackgroundBrush`）形成清晰视觉层级

**不一致的**：

- `InstanceActionCard` 没用 Card 或更高层级背景，显得比其他控件"浅"
- 专业页面（ProfileRules / DependencyGraph）信息密度显著高于日常页面，但按钮/操作样式保持一致，没有切换感——这是优点也是隐患

---

## 落地优先级建议

如果只改 3 件事：

1. **实例内的图标导航栏加上文字标签**（或当前页标题常驻）——新手留存的最大瓶颈
2. **创建实例流程补上"加载器选择"**，并把版本选择做成显式步骤——对齐玩家真实意图
3. **统一全局导航按钮风格 + 修掉 `Toolbar` 这类硬编码/未本地化文案**——低成本、立竿见影提升专业感

## 战略建议：功能分层

借设置里已有的 `Super Power` 开关做一次"分层"：

- **默认层**只露出 创建 / 启动 / Mod 管理 / 账户 / 设置 这些核心动线
- **高级层**收进 规则引擎、依赖图、查询语法、批量操作 等

这样既不损失高级用户的能力，又能让新玩家第一次打开时不被劝退。

---

## 涉及文件清单

核心动线：

- `src/Polymerium.Avalonia/MainWindow.axaml`
- `src/Polymerium.Avalonia/Pages/LandingPage.axaml`
- `src/Polymerium.Avalonia/Pages/NewInstancePage.axaml`
- `src/Polymerium.Avalonia/Pages/InstancePage.axaml`
- `src/Polymerium.Avalonia/Pages/InstanceHomePage.axaml`

功能页面：

- `src/Polymerium.Avalonia/Pages/InstanceSetupPage.axaml`
- `src/Polymerium.Avalonia/Pages/InstanceDashboardPage.axaml`
- `src/Polymerium.Avalonia/Pages/SettingsPage.axaml`
- `src/Polymerium.Avalonia/Pages/SnapshotManagementPage.axaml`

覆盖层与控件：

- `src/Polymerium.Avalonia/Modals/ProfileRulesModal.axaml`
- `src/Polymerium.Avalonia/Modals/ProfileRuleSelectorsModal.axaml`
- `src/Polymerium.Avalonia/Modals/InstanceDependencyGraphModal.axaml`
- `src/Polymerium.Avalonia/Controls/InstanceActionCard.axaml`
- `src/Polymerium.Avalonia/Controls/InstanceEntryButton.axaml`
- `src/Polymerium.Avalonia/Controls/AccountEntryButton.axaml`
- `src/Polymerium.Avalonia/Controls/InstancePackageButton.axaml`
- `src/Polymerium.Avalonia/Sidebars/NotificationSidebar.axaml`
