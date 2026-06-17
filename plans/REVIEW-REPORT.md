# 代码审查报告

**审查范围**：`origin/main..HEAD` 的 12 个未推送 commit  
**总变动**：54 文件，+2030 / -945 行  
**构建状态**：✅ 通过（0 Warning, 0 Error）

---

## 一、变更文件清单

### 1.1 新增文件（12 个）

| 文件 | 说明 |
|------|------|
| `plans/INSTANCE-STATE-AGGREGATOR.md` | 设计文档 |
| `plans/archived/MODAL-ACTION-PANEL.md` | 归档的设计文档 |
| `Models/InstanceStateSnapshot.cs` | 新模型：扁平状态快照 |
| `Models/QuickBarModel.cs` | 新模型：QuickBar 数据管理（Pinned + Active） |
| `Services/InstanceStateAggregator.cs` | 新服务：实例状态聚合器（唯一订阅者） |
| `Services/Sinks/ActivitySink.cs` | 新 Sink：操作/活动记录写入 |
| `Services/Sinks/CrashDiagnosisSink.cs` | 新 Sink：崩溃诊断通知 |
| `Services/Sinks/NotificationSink.cs` | 新 Sink：成功/失败/取消通知 |
| `SidebarModels/InstanceSidebarModel.cs` | 新 ViewModel：实例侧栏 |
| `SidebarModels/NotificationSidebarModel.cs` | 新 ViewModel：通知侧栏代理 |
| `Sidebars/InstanceSidebar.axaml` + `.cs` | 新 View：实例侧栏 |

### 1.2 删除文件（2 个）

| 文件 | 说明 |
|------|------|
| `Models/InstanceEntryState.cs` | 旧双枚举之一，已统一为 `InstanceState` |
| `Services/StateService.cs` | 空壳占位类，从未实现，已删除 |

### 1.3 修改文件（39 个）

#### 子模块

| 文件 | 变动摘要 |
|------|----------|
| `submodules/Trident.Net` | 指向新 commit：`InstanceState` 从 Core 上移到 Abstractions；TrackerBase 新增 `Kind`/`Progress`/`ReportProgress` 归一化 |

#### 核心服务 & 基础设施

| 文件 | 变动摘要 |
|------|----------|
| `Services/NotificationService.cs` | **大幅扩展**：从纯 growl 网关变成 `ObservableObject`，接管 `Notifications` 集合、`UnreadNotificationCount`、`MarkAllAsRead/MarkAsRead/MarkAsUnread/RemoveNotification` 命令、`PopNotification`/`ClearAll` 方法。原 `MainWindowContext` 中的通知逻辑全部搬入此服务。 |
| `Services/ConfigurationService.cs` | **新增** `Save()` 方法，将配置写入磁盘 |
| `Services/OverlayService.cs` | **新增** 泛型 `PopSidebar<TSidebar>()` 方法，通过 IViewActivator 创建 Sidebar 实例 |
| `Configuration.cs` | **新增** 常量 `QUICKBAR_PINNED_KEYS = "QuickBar.PinnedKeys"` 和属性 `QuickBarPinnedKeys` |
| `Startup.cs` | **新增** DI 注册：`InstanceStateAggregator`、`QuickBarModel`(×2)、`ActivitySink`、`NotificationSink`、`CrashDiagnosisSink` |
| `App.axaml.cs` | **改动**：`PopNotification` handler 不再指向 `MainWindowContext.PopNotification`，改为 `NotificationService.PopNotification`；启动时调用三个 Sink 的 `.Attach()` |

#### MainWindow 层

| 文件 | 变动摘要 |
|------|----------|
| `MainWindowContext.cs` | **大幅瘦身**（628 行删减到约 140 行）：删除了全部 4 个 `OnInstance*` 事件处理器（约 400 行）、通知管理方法（`PopNotification`、`MarkAllAsRead`、`MarkAsRead`、`MarkAsUnread`、`RemoveNotification`）、崩溃诊断 `BuildCrashReport`/`FindLatestCrashReport`（约 80 行）。改为订阅 `InstanceStateAggregator.StateChangeStream`，通过 `HandleSnapshotUpdate/HandleSnapshotRemove` 更新 `_entries`。新增 `QuickBar` 属性代理、`UnreadNotificationCount` 属性代理、`OpenInstanceSidebar` 命令。 |
| `MainWindow.axaml` | **改动**：原 "Toolbar" 区新增了 QuickBar 的 Active 和 Pinned 两个 `ItemsControl`；侧边栏按钮从单一 Accounts 变成 Accounts + Instance Grid 两个 Ghost 按钮；底部 `NotificationSidebar` 弹出方式从手动构造改为 `PopSidebar<NotificationSidebar>()` |

#### 控件 & 转换器

| 文件 | 变动摘要 |
|------|----------|
| `Controls/InstanceEntryButton.cs` | `InstanceEntryState` → `InstanceState` |
| `Controls/InstanceEntryButton.axaml` | ① `Preparing` → `Deploying` style selector；② 进度条 Value 绑定增加 `RatioToPercent` 转换器（0.0–1.0 → 0–100）；③ Tag Name 仍保留 `PreparingTag`（命名不一致） |
| `Controls/LaunchBar.cs` | 新增 `using TridentCore.Abstractions`（`InstanceState` 命名空间迁移） |
| `Converters/InternalConverters.cs` | **新增** `RatioToPercent` 转换器 |

#### 模型

| 文件 | 变动摘要 |
|------|----------|
| `Models/InstanceEntryModel.cs` | `InstanceEntryState` → `InstanceState`；新增 `using TridentCore.Abstractions` |
| `Models/InstanceEntryState.cs` | **已删除**（原 5 值枚举 Idle/Installing/Updating/Preparing/Running） |
| `Models/InstanceStateSnapshot.cs` | **新增**：`sealed record InstanceStateSnapshot(Key, State, Progress, Tracker?)` |

#### PageModels（全部改为接收 `InstanceStateAggregator` 并传递给基类）

| 文件 | 变动摘要 |
|------|----------|
| `InstancePageModelBase.cs` | **大幅重构**：删除了 `OnInstanceUpdating/Deploying/Launching` 等 3 个事件处理器及 `OnInstance*StateChanged` 回调（约 80 行）。改为 `_aggregator.Watch(Key)` 订阅，`_currentTracker` 引用跟踪，在 snapshot 变化时调 virtual hook。新增 `_aggregator` 字段和 `_aggregatorSubscription`。 |
| `InstancePageModel.cs` | **大幅瘦身**：删除了 `OnProfileUpdating/Deploying/Launching` + 3 个 StateChanged 回调（约 100 行）。改为 `_aggregator.Watch(Basic.Key).Subscribe(...)` 单行订阅。 |
| `InstanceHomePageModel.cs` | 基类参数加 `aggregator`，进度字段从 `DeployingProgressTotal` + `DeployingProgressCurrent` 合并为单 `DeployingProgress` |
| 其余 7 个 `Instance*PageModel.cs` | 构造函数新增 `InstanceStateAggregator aggregator` 参数并传给基类 |

#### Pages（AXAML 绑定调整）

| 文件 | 变动摘要 |
|------|----------|
| `InstanceDashboardPage.axaml` | `xmlns:trident` 命名空间从 `TridentCore.Core` → `TridentCore.Abstractions` |
| `InstanceHomePage.axaml` | 同上；ProgressBar 改为 `Maximum=1` + `Value={DeployingProgress}`；删除进度数字标注 |
| `InstanceSetupPage.axaml` | ProgressRing Value 增加 `RatioToPercent` 转换器 |

#### Sidebars

| 文件 | 变动摘要 |
|------|----------|
| `NotificationSidebar.axaml` | `x:DataType` 从 `MainWindowContext` → `NotificationSidebarModel`；所有命令绑定从 `$parent[app:NotificationSidebar].((app:MainWindowContext)DataContext)` → `$parent[app:NotificationSidebar].((app:NotificationSidebarModel)DataContext)` |
| `NotificationSidebar.axaml.cs` | 新增构造函数 `NotificationSidebar(NotificationSidebarModel model)` 供 Activator 路由 |

#### Modals（5 个）

| 文件 | 变动摘要 |
|------|----------|
| `AccountCreationModal.axaml` | 底部按钮区从 `StackPanel(Horizontal)` → `Grid(Col=Auto,*)` + `husk:ModalActionPanel(Layout=Right)`，Back 按钮在 Grid.Col=0 独立，其余按钮在 ActionPanel 内 |
| `AppUpdateModal.axaml` | 底部 Grid 列从 `*,Auto,Auto` → `*,Auto`，两个按钮包入 `husk:ModalActionPanel(Layout=Right)` |
| `OobeModal.axaml` | 同 AccountCreationModal，Back 按钮外置 + ActionPanel |
| `ProfileRulesModal.axaml` | Grid 列从 `Auto,*,Auto,Auto` → `Auto,*,Auto`；Add 按钮加 `IsDefault=True` 并移入 ActionPanel |
| `ProfileRuleSelectorsModal.axaml` | 同 ProfileRulesModal |

#### 其他

| 文件 | 变动摘要 |
|------|----------|
| `Polymerium.Avalonia.csproj` | Huskui.Avalonia 1.2.0 → 1.3.0；Huskui.Avalonia.Markdown/Mvvm 1.2.0 → 1.2.1 |
| `Properties/Resources.resx` | 新增 `InstanceSidebar_TitleText`="Instances"、`InstanceSidebar_SearchPlaceholder`="Search instances..." |
| `Properties/Resources.zh-hans.resx` | 新增 `InstanceSidebar_TitleText`="实例"、`InstanceSidebar_SearchPlaceholder`="搜索实例..." |
| `Properties/Resources.Designer.cs` | 自动生成的对应 Designer 属性 |
| `changelogs/rolling.md` | 新增条目 |

---

## 二、功能变动分析

### 2.1 消灭的功能（已替换）

| 原有功能 | 替换为 | 说明 |
|----------|--------|------|
| `InstanceEntryState` 枚举（5 值，`Preparing`） | `InstanceState` 枚举（TridentCore.Abstractions，`Deploying`） | 双枚举统一。`Preparing` → `Deploying` |
| `MainWindowContext.SubscribeState(InstanceManager)` 直接订阅 4 个事件 | `InstanceStateAggregator.StateChangeStream` | 主体从多个事件处理器变成单一聚合流订阅 |
| `MainWindowContext` 中 400+ 行的事件处理+通知+活动记录+崩溃诊断 | 3 个独立 Sink | 职责从 God Object 拆到独立服务 |
| `MainWindowContext.PopNotification` / 通知管理方法 | `NotificationService.PopNotification` + 命令 | 通知所有权从 MainWindowContext 移入 NotificationService |
| `MainWindowContext.BuildCrashReport` + `DiagnoseGameCrash` | `CrashDiagnosisSink.BuildCrashReport` | 崩溃诊断从 MainWindowContext 拆出 |
| `MainWindowContext.Notifications` / `UnreadNotificationCount` | `NotificationService.Notifications` / `UnreadNotificationCount` | 集合从页面级提升为应用级单例 |
| `NotificationSidebar` 直接绑定 `MainWindowContext` | `NotificationSidebarModel` 代理 | 遵循 MVVM separating pattern |
| `MainWindowContext` 手动构造 `new NotificationSidebar()` | `OverlayService.PopSidebar<NotificationSidebar>()` | 通用化 Sidebar 弹出方式 |
| `InstancePageModel` + `InstancePageModelBase` 各自订阅 InstanceManager 事件 | 两层统一订阅 `InstanceStateAggregator.Watch(key)` | 消除了子类+基类的重复订阅 |
| Tracker 进度异构流（`Progress<double?>` / `Progress<(int,int)>` / `StageStream`） | `TrackerProgress` 归一化（None/Indeterminate/Determinate）+ `TrackerProgress.Percent` 0.0–1.0 | 归一化下沉到 Tracker 层 |
| 模态框手排版按钮 | `husk:ModalActionPanel` 平台感知布局 | macOS 适配（POLY-101） |

### 2.2 新增功能

| 功能 | 说明 |
|------|------|
| **InstanceStateAggregator** | 应用级单例，作为 InstanceManager 4 个 tracker 事件的唯一订阅者，将异构 tracker 进度压平成 `InstanceStateSnapshot` 变化流，通过 `StateChangeStream`（DynamicData SourceCache）和 `Watch(key)`（单实例 Observable）对外广播 |
| **InstanceStateSnapshot** | 扁平 record：`(Key, State, Progress, Tracker?)`。常规 UI 只需 State + Progress，需细粒度信息时下转型 Tracker |
| **Sink 基础设施** | ActivitySink（操作/活动记录写入 PersistenceService）、NotificationSink（通知弹窗）、CrashDiagnosisSink（崩溃诊断弹窗）—— 均订阅 Aggregator 的 StateChangeStream |
| **QuickBar** | MainWindow 底部工具栏下方新增 Active（运行中实例）和 Pinned（用户钉选实例）两个区域。`QuickBarModel` 管理 PinnedKeys 持久化（ConfigurationService）+ Active 过滤（排除 Pinned） |
| **InstanceSidebar** | 左侧边栏新增实例入口弹出面板，含搜索框、Loader type chips、实例列表 |
| **NotificationSidebarModel** | 代理 NotificationService 的属性/命令，避免 View 直接暴露 Service |
| **OverlayService.PopSidebar\<TSidebar>** | 泛型 Sidebar 弹出，通过 IViewActivator 创建实例 |
| **ConfigurationService.Save()** | 公开方法，将内存配置写入磁盘（QuickBar PinnedKeys 需要） |
| **InternalConverters.RatioToPercent** | 0.0–1.0 → 0–100 转换器，适配进度条百分比绑定 |
| **Huskui.Avalonia 升级** | 1.2.0 → 1.3.0（ModalActionPanel 新组件） |
| **Trident.Net 子模块更新** | `InstanceState` 上移到 Abstractions；`TrackerBase` 新增 `Kind`/`Progress`/`ProgressChanged`/`ReportProgress()` |

---

## 三、审查结果

### 🔴 严重问题

#### 1. QuickBar PinnedKeys 持久化不写入磁盘
**文件**: `Models/QuickBarModel.cs:108-111`

`SavePinnedKeys()` 只修改了内存中 `ConfigurationService.Value.QuickBarPinnedKeys`，但从未调用 `ConfigurationService.Save()` 写入磁盘。用户钉选的实例在应用重启后会丢失。

```csharp
private void SavePinnedKeys()
{
    var json = JsonSerializer.Serialize(_pinnedKeys.ToList());
    _configurationService.Value.QuickBarPinnedKeys = json;
    // ← 缺少 _configurationService.Save();
}
```

**严重性**: 功能性 bug，用户数据丢失。

---

#### 2. Sink.Attach() 订阅返回值被丢弃
**文件**: `Services/Sinks/ActivitySink.cs:17`, `NotificationSink.cs:27`, `CrashDiagnosisSink.cs:29`

三个 Sink 的 `Attach()` 方法对 `aggregator.StateChangeStream.Subscribe(...)` 的返回 `IDisposable` 未保存，也没有提供 `Detach()` 方法。

```csharp
public void Attach()
{
    aggregator.StateChangeStream
              .Subscribe(change => { ... }); // IDisposable 丢失
}
```

**影响**: Sink 与 Aggregator 同为 Singleton 在生产中不会 GC 泄漏，但违反 Rx 最佳实践，且完全不可测试（无法停止订阅、无法验证生命周期）。

---

#### 3. InstanceSidebarModel 搜索 + Chip 过滤功能未实现
**文件**: `SidebarModels/InstanceSidebarModel.cs`

`SearchText` 和 `SelectedChip` 是 `ObservableProperty`，但没有任何响应逻辑。修改搜索文字不会过滤 `Instances`，选择 Chip 也不会过滤 `Instances`。AXAML 中也没有实现过滤绑定。

UI 上搜索框和 Chip 按钮是可见的，用户交互后不会产生任何效果。

**影响**: 功能半成品，用户可见但不可用的 UI 元素。

---

### 🟡 设计问题

#### 4. `IsProcessFaulted()` 在两个 Sink 中重复定义
**文件**: `CrashDiagnosisSink.cs:182`, `NotificationSink.cs:167`

两处是完全相同的 private static 方法，模式匹配逻辑也相同。应提取为共享工具方法（如 `ExceptionHelper.IsProcessFaulted`）。

---

#### 5. `InstanceStateSnapshot.Tracker` 可空但消费方用 `!` 操作符
**文件**: `Models/InstanceStateSnapshot.cs:14`, `Models/QuickBarModel.cs:46`

Snapshot 定义 `TrackerBase? Tracker` 为可空，但 `QuickBarModel` 排序时：
```csharp
.Sort(SortExpressionComparer<InstanceStateSnapshot>.Descending(s => s.Tracker!.StartedAt))
```

用了 null-forgiving 操作符。虽然 Active 过滤理论上不会包含 Null tracker（Remove 时已移除），但这是隐含假设而非显式约束。

---

#### 6. MainWindowContext.SubscribeState 线程安全风险
**文件**: `MainWindowContext.cs:625-685`

`HandleSnapshotUpdate` 中 `_entries.Items.FirstOrDefault(...)` 在非 UI 线程执行查询，而 `_entries.AddOrUpdate(model)` 在 `Dispatcher.UIThread.Post` 内执行。`SourceCache<T,K>` 的线程安全取决于内部实现，如果查询和修改竞争可导致不一致。

---

#### 7. NotificationSidebarModel 过度暴露内部集合
**文件**: `SidebarModels/NotificationSidebarModel.cs:30-31`

```csharp
public ObservableCollection<NotificationModel> Notifications => _notificationService.Notifications;
```

直接暴露 `NotificationService.Notifications` 的引用，View 或 ViewModel 可绕过 SidebarModel 直接操作集合。如果代理的目的是封装，则应考虑更严格的边界。

---

#### 8. Startup.cs 中 QuickBarModel 注册了两次
**文件**: `Startup.cs:121,131`

```csharp
.AddSingleton<QuickBarModel>()  // line 121
// ...
.AddSingleton<QuickBarModel>()  // line 131
```

DI 容器取最后一次注册（last wins），功能上可能碰巧正确，但这是明显的 copy-paste 错误。应删除其中一个。

---

### 🟡 实现偷懒

#### 9. ActivitySink 依赖 Aggregator 内部实现细节
**文件**: `Services/Sinks/ActivitySink.cs:27-34`

Sink 在 `ChangeReason.Remove` 时读取 `item.Current.Tracker` 来判断最终状态。但 Remove 事件传递的是 "此 key 回到 Idle"，而不是 "最终 snapshot"。当前碰巧工作是因为 Aggregator 只在 Finished/Faulted 时才 Remove，但这依赖了 Aggregator 的未文档化行为。

---

#### 10. CrashDiagnosisSink 的 Diagnose 命令闭包捕获已完成 tracker
**文件**: `Services/Sinks/CrashDiagnosisSink.cs:82-87`

`RelayCommand(() => Diagnose(launcher))` 闭包捕获 `launcher`。虽然 tracker 对象在 CLR 层面仍被引用所以可用，但某些底层资源（如 Process 对象）在 tracker 完成后可能已释放。

---

#### 11. InstanceEntryButton.axaml Tag 仍命名为 "PreparingTag"
**文件**: `Controls/InstanceEntryButton.axaml:128`

`InstanceState` 枚举已从 `Preparing` 改为 `Deploying`，Style Selector 也已更新为 `^[State=Deploying]`，但 Tag 控件的 `Name="PreparingTag"` 保留旧名。建议重命名为 `DeployingTag` 以保持一致性。

---

#### 12. QuickBar UI 标签 "QuickBar" 硬编码未国际化
**文件**: `MainWindow.axaml:254`

```xml
<TextBlock ... Text="QuickBar" />
```

之前 "Toolbar" 也是硬编码的（历史遗留），但既然在改此区域，新文本也应走 `Resources.resx`。

---

#### 13. InstanceSidebar 的 InstanceBasicModel 缺少 Thumbnail
**文件**: `Sidebars/InstanceSidebar.axaml:60-75`

AXAML 绑定 `{Binding Thumbnail, FallbackValue={x:Null}}`，但 `InstanceBasicModel` 构造函数不包含 thumbnail 参数，实例图标不会显示。

---

#### 14. SavePinnedKeys 序列化空集行为与默认值不一致
**文件**: `Models/QuickBarModel.cs:108-111`

`_pinnedKeys` 为空时序列化输出 `[]`（JSON 数组），而 `Configuration.QuickBarPinnedKeys` 默认值是 `""`（空字符串）。`LoadPinnedKeys` 用 `string.IsNullOrEmpty` 检查所以功能无碍，但语义不一致。

---

### ✅ 做得好的地方

1. **Aggregator 模式设计清晰**：注释详尽，惟一真源架构，StateChangeStream + Watch(key) 双层 API
2. **TrackerProgress 归一化下沉到 Tracker 层**：消除了消费方的 switch(tracker) 进度解析
3. **Sink 拆分职责明确**：ActivitySink（持久层）、NotificationSink（通知弹窗）、CrashDiagnosisSink（崩溃诊断）各司其职
4. **Snapshot 扁平化**：消费方只需 `(Key, State, Progress)` 三元组，不需要接触异构 StageStream/ProgressStream
5. **MainWindowContext 瘦身**：从 600+ 行降到约 140 行，消除 God Object
6. **NotificationService 提升为应用级单例**：通知集合不再绑定窗口生命周期
7. **OverlayService 泛型化**：`PopSidebar<T>()` 消除手动构造模式
8. **InstanceEntryState → InstanceState 统一**：彻底消灭双枚举
9. **ModalActionPanel macOS 适配**：整洁的平台感知动作布局

---

### 修复优先级总结

| # | 严重度 | 问题 | 建议 |
|---|--------|------|------|
| 1 | 🔴 Critical | PinnedKeys 不持久化到磁盘 | `SavePinnedKeys()` 中加 `_configurationService.Save()` |
| 2 | 🔴 High | Sink.Attach() 订阅泄漏 | 保存 IDisposable，提供 Detach() 或文档说明同生命周期 |
| 3 | 🔴 Medium | Sidebar 搜索+Chip 过滤未实现 | 实现过滤逻辑或从 UI 移除搜索框/Chip |
| 8 | 🟡 Medium | QuickBarModel 重复注册 | Startup.cs 删除重复行 |
| 4 | 🟡 Low | IsProcessFaulted 重复 | 提取到共享 ExceptionHelper |
| 11 | 🟡 Low | PreparingTag → DeployingTag 重命名 | AXAML 中 Rename tag Name |
| 12 | 🟡 Low | "QuickBar" 文本未国际化 | 走 Resources.resx |
| 13 | 🟡 Low | InstanceBasicModel 缺 Thumbnail | 构造函数加 thumbnail 参数或用占位图 |
| 5 | 🟡 Low | Tracker! null-forgiving | 加注释或改用安全排序 fallback |