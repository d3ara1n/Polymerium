# 主窗口解耦与 macOS 窗口生命周期

> **状态**: 进行中（Step 1、2 已完成，Step 3 待 POLY-23 启动后实施）
> **关联 Issue**: [POLY-102](https://d3ara1n.atlassian.net/browse/POLY-102)（已完成）→ [POLY-23](https://d3ara1n.atlassian.net/browse/POLY-23)（阻塞）→ [POLY-99](https://d3ara1n.atlassian.net/browse/POLY-99)
> **Issue 依赖链**: `POLY-99 (macOS 窗口) ──is blocked by──> POLY-23 (InstanceDrawer/pinned 实例)`
> **版本规划**: POLY-102 / POLY-99 → v1.10.0；POLY-23 → v1.11.0

---

## 背景与动机

Polymerium 在 macOS 上的关闭行为不符合平台习惯：用户点窗口关闭按钮会直接退出整个程序，而非 macOS 惯例的「关窗不关软件、Dock 可重开窗口」。要修正这一点（POLY-99），需要在 macOS 上实现「关窗销毁 → Dock 点击重建主窗口」。

但调查发现，主窗口（`MainWindow`）此前承担了过多职责，通过静态 `MainWindow.Instance` 单例被全仓 25 处直接引用，使得「销毁/重建窗口」这条路径上有两个硬骨头：

1. **静态单例的空窗期问题**：窗口销毁后、新窗口建好前，任何后台任务（下载完成弹通知等）若触发走 `MainWindow.Instance` 都会 NRE。
2. **ViewModel 的状态泄漏**：`MainWindowContext` 持有实例列表的全部状态（`SourceCache` + 4 个 instance tracker 订阅 + profile 订阅），但其 `OnDeinitialize()` 反订阅不完整，反复创建会泄漏。

因此本任务**先做接入面解耦**，为后续的窗口重建扫清障碍。解耦分三步走，且与 InstanceDrawer 改版（POLY-23）天然耦合——POLY-23 本身会大幅瘦身 `MainWindowContext`。

---

## 任务分解

### Step 1：消除 `MainWindow.Instance`（A 类 17 处 TopLevel 调用）✅ 已完成

**目标**：把「找一个具体的 MainWindow」降级为「找当前活跃的 TopLevel」，消除类型耦合的全局单例。

**做法**：在 `TopLevelHelper` 加无状态静态方法 `GetTopLevel()`，从 `IClassicDesktopStyleApplicationLifetime.MainWindow` 实时查询；无窗口时 `throw new UnreachableException`（不应发生，靠 `AppDomain.UnhandledException` 全局兜底转 Sentry，不主动 Report）。

**改动清单**（commit `70efa717`）：

| 文件 | 改动 |
|------|------|
| `Utilities/TopLevelHelper.cs` | 新增 `GetTopLevel()` 静态方法 |
| `Models/InternalCommands.cs`(4)、`MainWindowContext.cs`(3)、`PageModels/*`(8 处) | `TopLevel.GetTopLevel(MainWindow.Instance)` → `TopLevelHelper.GetTopLevel()` |
| `Dialogs/ModpackExporterDialog.axaml.cs` | 内部消化为 `TopLevel.GetTopLevel(this)`（它是 Control，就近取） |

### Step 2：建立 `ThemeService` 并消化 B 类 `MainWindow.Instance`（8 处）✅ 已完成

**目标**：把主题/外观状态的「被动接口」换成业务层「唯一权威」，窗口是其订阅消费方。彻底删除 `MainWindow.Instance` 属性与 4 个 `SetXxx` 方法。

**做法**：新建 `ThemeService`，6 个普通 C# 属性 + `event EventHandler? ThemeChanged`。setter 内部判等变化→写配置→触发事件。`MainWindow.AttachTheme(theme)` 订阅事件后 `ApplyTheme` 重新读取全部状态应用。SettingsPage / OOBE 只改 `ThemeService` 属性，不再碰 `ConfigurationService` 的主题 key 或窗口。

**改动清单**（commit `70efa717`）：

| 文件 | 改动 |
|------|------|
| `Services/ThemeService.cs` | **新建**：6 属性（Accent/ThemeVariantIndex/TransparencyIndex/Corner/TitleBarVisible/LeftPanelMode）+ ThemeChanged |
| `MainWindow.axaml.cs` | 删 `Instance` 静态属性 + `SetColorVariant`/`SetCornerStyle`/`SetThemeVariantByIndex`/`SetTransparencyLevelHintByIndex` 4 方法；新增 `AttachTheme`/`ApplyTheme` |
| `App.axaml.cs` | `ConstructWindow` 的 6 行外观设置 → `window.AttachTheme(themeService)` |
| `Startup.cs` | `AddSingleton<ThemeService>()` |
| `PageModels/SettingsPageModel.cs` | 注入 `ThemeService`，6 个 `OnXxxChanged` 改 `_themeService.Xxx = value`（不再写配置、不碰窗口）|
| `Modals/OobeModal.axaml.cs` | 创建 `OobeQuickSetup` 时注入 `ThemeService` |
| `Components/OobeQuickSetup.axaml.cs` | `required ThemeService`，2 个 setter 改走 `ThemeService` |

### Step 3：实例列表状态抽离到专门服务 ⏳ 待实施（与 POLY-23 合并）

**目标**：把 `MainWindowContext` 里约 **550 行**的实例列表管理整体搬到一个专门服务（用户倾向合入 `InstanceService`，但因 POLY-23 推迟，本步也往后推）。这样主窗口无需总是重建，且该服务还要能提取出 `PinnedInstances`，为 InstanceDrawer 铺路。

**待搬走的内容**（`MainWindowContext.cs`）：

- `_entries` SourceCache + filter pipeline + `View` 集合（行 44、83-91、182-186）
- `FilterText` 属性（行 188）
- `SubscribeProfileList` + `OnProfileAdded/Updated/Removed`（行 604-665）
- `SubscribeState` + `OnInstanceInstalling/Updating/Deploying/Launching`（行 681-1046，约 365 行）
- `ExportInstanceAsync` 及相关命令

**POLY-23 描述里的明确目标**（原话）：

> 如果出了 InstanceDrawer，那么首页的列表就改成 Pinned Instances 含义，只显示钉住的实例以及最近添加的N个，并且不显示状态，去掉搜索，保留排序（如果可以把 MainWindowContext 里的一坨状态跟踪都去掉，那排序也不需要了）。

**落地后的效果**：`MainWindowContext` 瘦身后只剩纯 UI 状态（`Notifications` 集合、`UnreadNotificationCount`、`CurrentUpdate`），关窗重建零成本、零泄漏风险。

---

## 最终目标：POLY-99 macOS 窗口重建

三步解耦完成后，实现 macOS 正确行为所需的三件事：

1. **关窗不退出**：`desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown`（仅 macOS）。
2. **Dock 重开窗口**：`Application.TryGetFeature<IActivatableLifetime>()` 的 `Activated` 事件，监听 `ActivationKind.Background`（Avalonia 11.1+ 的官方 API，对应 macOS `applicationShouldHandleReopen`）。
3. **真正退出入口**：补 NativeMenu 的 Quit 项 / Cmd+Q → `Program.Terminate`。

**三步解耦为 POLY-99 扫清的障碍**：
- `MainWindow.Instance` 已消失 → 无空窗期 NRE 风险（实时查 lifetime，无窗口直接 panic）。
- `ThemeService` 持有状态 → 新窗口构造时 `AttachTheme` 自动恢复全部外观，无需关窗前保存。
- Instance 列表抽离（Step 3）后 → `MainWindowContext` 无重订阅泄漏风险。

**POLY-99 实施时仍需注意的点**：
- `MainWindowContext.OnDeinitialize()` 当前反订阅不完整（漏了 ProfileManager/InstanceManager 的 7 个事件订阅 + reactive pipeline）。Step 3 完成后这些代码被整体移走，问题自然消失；若 Step 3 未做就先上 POLY-99，必须先补全。
- `MainWindow.AttachTheme` 订阅了 `ThemeChanged` 但没有取消订阅。单窗口场景无问题，窗口重建时需在 `window.Closed` 取消订阅。
- `NavigationService` 的历史在 `Frame` 控件里（窗口内），关窗会丢失 → 重开回到 `LandingPage`，符合预期。

---

## 设计约定（务必遵守）

本轮工作中确立、日后继续必须遵守的几条原则：

1. **Service 层不允许 Reactive**。`[ObservableProperty]` / `ObservableObject` 是纯 MVVM 的，属于界面而非业务。Service 层做通知用事件。本任务中 `ThemeService` 即为范例：普通属性 + `event EventHandler? ThemeChanged`。
2. **事件统一一个，不给快照**。`ThemeChanged` 用 `EventArgs.Empty`，消费方收到通知后自行到 Service 读取当前状态（变了更新、没变不更新）。Service 完全不知道谁在消费。
3. **能内部消化的就不调用外部 helper**。这是风格与哲学。例如 `ModpackExporterDialog` 本身是 Control，就用自己的 `TopLevel.GetTopLevel(this)`，而非绕道全局 `TopLevelHelper.GetTopLevel()`。
4. **预料之外的 exceptional 直接 `throw new UnreachableException`**。崩溃会被 `AppDomain.UnhandledException`（`App.axaml.cs` 已挂全局钩子转发 Sentry）捕获，无需多此一举主动 `ErrorReporter.Report`——除非根本不打算报错。
5. **步骤化、先提案后行动**。复杂改造分阶段确认，每步独立编译验证、独立提交。

---

## 进度速览

| 步骤 | 内容 | 关联 Issue | 状态 |
|------|------|-----------|------|
| Step 1 | 消除 `MainWindow.Instance`（A 类 17 处） | POLY-102 | ✅ commit `70efa717` |
| Step 2 | `ThemeService` 门面 + B 类 8 处 | POLY-102 | ✅ commit `70efa717` |
| Step 3 | Instance 列表状态抽离（合入 `InstanceService`） | POLY-23 | ⏳ 待 v1.11.0 |
| 最终 | macOS 窗口重建 | POLY-99 | ⏳ 待 Step 3 / POLY-23 |

**下一步行动**：等待 POLY-23（InstanceDrawer / pinned 实例）启动，届时一并完成 Step 3，随后即可实施 POLY-99 的 macOS 窗口生命周期。
