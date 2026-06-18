# macOS 浮动侧边栏与窗口圆角风格化

> **状态**: 草案（窗口底色方案、交通灯处理待调研）
> **适用平台**: macOS 专享；Windows / Linux 保持现状
> **关联**: 触及 `MainWindow`、`Configuration`、Page 基类 ControlTheme

---

## 背景与动机

需求是让 macOS 下侧边栏以「浮动面板」形态呈现（圆角 + 半透明背景 + 四周留白，类似 macOS Notes/Music 的悬浮卡片），其余平台保持当前贴边双列布局。

调研后发现这个需求和一个被忽略的前提强耦合：**浮动 sidebar 不能脱离「窗口大圆角」单独做**。

### 现状关键事实

| 维度 | 现状 | 影响 |
|------|------|------|
| Sidebar 布局 | `MainWindow.axaml` 顶层 `Grid Container` 双列 `83*/27*`，`Sidebar` 是普通 `DockPanel`，无 Background、无 CornerRadius | 没有任何浮动卡片的基础结构 |
| macOS 窗口 chrome | `Configuration.cs:43` `APPLICATION_TITLE_BAR` 默认 `OperatingSystem.IsWindows()`，故 macOS 下 `IsTitleBarVisible=false` → `ExtendClientAreaToDecorationsHint=false`，走**系统原生窗口** | 窗口圆角由 macOS 系统强制（~10px），Avalonia/Huskui 改不动 |
| 圆角设置 | `Configuration.cs:51` macOS 默认 `CornerStyle.Large`，作用于 `appTheme.Corner` | **只影响按钮/卡片等内部控件**，不触及窗口四角 |
| Page 背景 | 子 Page 未改背景，统一来自 `Page`/`Subpage`/`ScopedPage` 基类的 ControlTheme | 浮动布局下会盖住窗口底色，主区域透不出来 |

### 核心结论

如果只在 sidebar 层套一个 `Border`（圆角 + 半透明），而窗口本身仍是系统 ~10px 小圆角，**四角会明显割裂**：窗口圆角小、内部卡片圆角大，两层圆角对不齐，反而比现在更难看。macOS 原生应用看着舒服的「大圆角」，本质是**窗口本身的大圆角**撑起来的，内部卡片反而常常没有独立圆角。

因此 **切入点在窗口层（去 chrome + 外层剪裁 Border），而不是 sidebar 层**。从 sidebar 切必割裂。

---

## 目标视觉形态（仅 macOS）

| 层 | 目标 |
|----|------|
| 窗口 chrome | 去掉系统 titlebar（或延伸内容、保留交通灯），为自定义圆角让路 |
| 窗口四角 | 整个内容套大圆角 `Border` + `ClipToBounds`，自己剪出 ~12–14px 圆角 |
| Sidebar | 浮动卡片：`Border`（CornerRadius + `OverlayHalfBackgroundBrush` + 四周 Margin） |
| 主内容区 | 透出窗口底色（去除 Page 基类背景） |
| 其他平台 | 完全不变 |

---

## 实施路径（自顶向下分层）

### Layer 0 — 窗口底色 ⏳ 待调研

去 chrome 后窗口需要一层底色供 sidebar 半透明背景和主区域叠加。

> **待定**：Mica / AcrylicBlur / 纯主题色 / Mica→Acrylic 降级链。由负责人调研 Avalonia 在 macOS 的实际可用性与性能后填入。当前 `ThemeService.TransparencyIndex` 已有四档（Mica/Acrylic/Blur/None），可复用现有配置面。

底色方案影响 Layer 2 的剪裁 Border 是否需要透明背景，需先定。

### Layer 1 — macOS 窗口去 chrome

**决策点**：macOS 去掉系统 titlebar 后，**红绿灯交通灯按钮怎么办**。两条路：

- **A. 完全自绘窗口按钮**：复用现有 `TitleBar`（`MainWindow.axaml` 里已有 minimize/maximize/close 三按钮，仅 `IsTitleBarVisible` 控制显隐）。改动小，但失去 macOS 原生交通灯交互习惯。
- **B. 保留交通灯 + 内容延伸**：用 `ExtendClientAreaToDecorationsHint=true` 让内容延伸到标题栏区域，但保留系统交通灯（需 `ExtendClientAreaChromeHints` 配合）。更贴合 macOS 习惯，但 sidebar 浮动卡片的上边距要给交通灯让位。

> 建议倾向 B（更符合 macOS 原生浮动 sidebar 应用的做法，如 Notes/Mail 都保留交通灯）。但 B 在 Avalonia macOS 上的行为需实测验证（参见已知 issue #10650 ExtendClientArea 与 SystemDecorations 交互）。

**改动点**：
- `Configuration.cs:43` `APPLICATION_TITLE_BAR` 默认值改为 `OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()`，或保持默认、在 `ApplyTheme` 里做平台分支。
- `MainWindow.axaml.cs` `OnPropertyChanged(IsTitleBarVisibleProperty)` 分支里按平台区分 `ExtendClientAreaChromeHints`。

### Layer 2 — 窗口内容大圆角剪裁

在 `MainWindow.axaml` 顶层 `Grid Container` 外（即 `AppWindow.Content` 直接子级）套一层：

```xml
<Border x:Name="WindowClip"
        CornerRadius="{平台分支: macOS=14, 其它=0}"
        ClipToBounds="True">
    <Grid Name="Container"> ... </Grid>
</Border>
```

- 仅 macOS 启用大圆角，其它平台 `CornerRadius=0`（行为不变）。
- 平台分支可用 `Classes="macos"` + Style，或 code-behind 在构造函数按 `OperatingSystem.IsMacOS()` 设值。
- **注意**：窗口最大化/全屏时圆角应归零，否则四角有黑边。需监听 `WindowState` 调整。

### Layer 3 — Sidebar 浮动卡片

把 `Sidebar` DockPanel 包进一层 `Border`：

```xml
<Border x:Name="SidebarCard"
        Classes.floating="macos"
        CornerRadius="12"
        Background="{StaticResource OverlayHalfBackgroundBrush}"
        Margin="8"
        Padding="0">
    <DockPanel x:Name="Sidebar" Grid.Column="1"> ... </DockPanel>
</Border>
```

- **对 `ApplySidebarPlacement` 的影响**：当前方法操作 `Sidebar.GetValue(Grid.ColumnProperty)` 和 `Main/Sidebar` 的 `Grid.Column`。引入外层 `Border` 后，赋值 `Grid.ColumnProperty` 的目标应改为 `SidebarCard`（或 Border 与 DockPanel 同步）。这是本层唯一需要动 code-behind 的地方。
- `Margin="8"` 制造四周留白；`OverlayHalfBackgroundBrush` 是项目内 3 个控件已在用的半透明画刷，无需新增资源。
- 非 macOS 平台用 `Classes` 切回无 margin、无背景、无圆角（或直接不套 Border）。

### Layer 4 — 主区域透底

去掉 Page 基类背景，让窗口底色透出。

- **背景来源**：子 Page（`LandingPage`、`SettingsPage`、`InstancePage` 等）均继承 `Page` / `Subpage` / `ScopedPage`，自身不改 Background，统一由基类 ControlTheme 决定。
- **ControlTheme 位置待核实**：`Themes/Controls.axaml` 通过 `<ResourceInclude Source="/Controls/Page.axaml" />` 与 `/Controls/ScopedPage.axaml` 引用，但当前工作区 `Themes/Controls/` 目录下未找到对应文件，需确认这些 ControlTheme 是在项目内、还是落在 Huskui 包内（若是包内则需用 Style 覆盖而非改源）。
- **做法**：为 `Page`/`Subpage`/`ScopedPage` 增加一个平台变体样式（macOS 下 `Background="{x:Null}"` 或 Transparent），其它平台保留原背景。用 `Style` + 平台 Class 或运行时注入。
- **风险**：部分 Page 内部子元素用了 `LayerBackgroundBrush`/`OverlaySolidBackgroundBrush` 作为大块底色（如 `InstancePage.axaml:52`、`InstanceSetupPage.axaml:130`、`InstancePropertiesPage.axaml:69`），这些在 macOS 下是否也需要透底需逐页判断，否则主区域会出现「外层透底、内层一块实色」的断层。

---

## 关键文件清单

| 文件 | 涉及层 | 改动 |
|------|--------|------|
| `src/Polymerium.Avalonia/MainWindow.axaml` | L2/L3 | 顶层加剪裁 Border；Sidebar 外包浮动卡片 Border |
| `src/Polymerium.Avalonia/MainWindow.axaml.cs` | L1/L2/L3 | 平台分支；`ApplySidebarPlacement` 改操作目标；WindowState 监听圆角归零 |
| `src/Polymerium.Avalonia/Configuration.cs` | L1 | `APPLICATION_TITLE_BAR` 平台默认值 |
| `src/Polymerium.Avalonia/Themes/Controls.axaml` 及 Page 基类 ControlTheme | L4 | macOS 变体去背景（位置待核实） |
| 可能新增 `Themes/Platform.macOS.axaml` | L2/L3/L4 | 统一收口 macOS 平台样式（可选） |

---

## 平台判断入口（统一）

项目现有惯例：`OperatingSystem.IsMacOS()` 静态方法，零依赖。`Configuration.cs`、`App.axaml.cs`、`SettingsPageModel.cs` 均如此。本计划沿用，不引入新 Helper。

样式层面的平台分支推荐用 Control `Classes`（如 `Classes.Add("macos")` 在 MainWindow 构造函数按平台加），避免在 XAML 里散落 `OperatingSystem` 判断。

---

## 风险与兼容性

1. **macOS borderless 窗口行为**：拖拽、resize、全屏（`WindowState.FullScreen`）、Spaces 行为可能与系统窗口不同，需实测。已知 Avalonia issue #10650 记录了 `ExtendClientAreaToDecorationsHint` 与 `SystemDecorations` 在 macOS 的交互问题。
2. **圆角与最大化冲突**：Layer 2 的剪裁圆角在全屏/最大化时必须归零，否则四角漏黑。
3. **跨平台隔离**：所有改动必须以平台判断严格收口，Windows/Linux 走原路径，避免回归。
4. **Page 背景断层**：Layer 4 逐页排查内部大块底色，避免透底不一致。
5. **`ApplySidebarPlacement` 重构**：Layer 3 引入外层 Border 后，`Grid.Column` 赋值目标变更，是逻辑易错点。

---

## 下一步行动项

- [ ] **负责人调研**：窗口底色方案（Layer 0）
- [ ] **负责人决策**：交通灯处理（Layer 1 的 A/B 路线）
- [ ] **核实**：Page 基类 ControlTheme 实际位置（Layer 4）
- [ ] 定下上述三项后，更新本文档并进入实施
