# InstanceStateAggregator：实例状态聚合层

> **状态**：设计中（待确认后实施）
> **关联 Issue**：[POLY-23](https://d3ara1n.atlassian.net/browse/POLY-23)（InstanceSidebar / QuickBar 的前置基础设施）
> **关联计划**：`plans/MAINWINDOW-DECOUPLING.md` Step 3（本计划完成其「状态跟踪抽离」的先决条件）
> **版本规划**：v1.11.0

---

## 背景与动机

实例的运行时状态（Installing / Updating / Deploying / Running）目前被**重复订阅 3-4 处**，且每处都要自己解析 Tracker 的异构进度流：

| 订阅点 | 订阅的事件 | 额外做的事 |
|--------|-----------|-----------|
| `MainWindowContext.SubscribeState`（约 365 行） | 4 个 InstanceManager 事件全订 | 创建/更新 entry、进度解析（4 种单位）、通知、活动记录、崩溃诊断 |
| `InstancePageModelBase`（基类） | Updating/Deploying/Launching 3 个 | key 过滤 + State 机 + virtual hooks |
| `InstancePageModel`（壳页面） | 同上 3 个 | **与基类几乎完全重复**（开发者自留注释 `// 终究还是得有个 InstanceStateAggregator`） |
| `InstanceHomePageModel` / `InstanceDashboardPageModel` | 通过基类 virtual hook | 各自订阅进度细节 / 进程监控 |

根本问题是状态信息的**异构嵌套**：消费方要拿到「这个实例在干什么、进度多少」，得从 `Tracker →（StageStream + ProgressStream）→ 归一化` 三层分别取，且 4 种 Tracker 的进度单位互不兼容：

- `InstallTracker.ProgressStream` → `Subject<double?>`
- `UpdateTracker.ProgressStream` → `Subject<double?>`
- `DeployTracker.ProgressStream` → `Subject<(int,int)>`（current/total）+ `Subject<DeployStage>` StageStream
- `LaunchTracker` → 无进度流，只有 `Process` / `ProcessAssigned` / `ScrapStream`

外加两个遗留伤痕：
1. **双枚举**：`InstanceEntryState`（列表用，有 `Preparing`）与 `InstanceState`（Trident 用，有 `Deploying`）语义相同、值名不一。
2. **`StateService.cs` 空壳**：开发者当初预留的占位，注释写明「替代 InstanceStateAggregator」，但从未实现。

本计划的目标：建一个应用级单例 `InstanceStateAggregator`，成为 `InstanceManager` 4 个事件的**唯一订阅者**，把异构 Tracker 状态**归一化下沉到 Tracker 层**、聚合成扁平 snapshot 对外广播，终结上述全部重复订阅。

---

## 核心设计

### 设计原则（务必遵守）

1. **Aggregator 只管「订阅 + 解析」，不管「模型更新与表现」**。消费方自己把 snapshot 投影成自己的 `InstanceEntryModel` → `InstanceEntryButton`。
2. **归一化下沉到 Tracker 层（Trident 子模块），而非 Aggregator 反向工程**。每种 Tracker 最懂自己的进度语义，归一化知识留在它内部。Aggregator 零 `switch(tracker)`。
3. **复用 `InstanceState` 枚举，废弃 `InstanceEntryState`**。所有状态统一用 Trident 的 `InstanceState`，消灭双枚举。
4. **Snapshot 是扁平的三元组**：`(Key, State, Progress)`，不再有 `Tracker → Stage → Progress` 嵌套。原始 Tracker 引用保留在 snapshot 里供「需要更细信息」的消费方钻取（Dashboard 取 Process、HomePage 取 DeployStage enum）。

### 归一化模型：TrackerProgress（record 三层）

进度本质有三种态，用 record 表达（非视图模型类，不需绑定通知——消费方自己桥接）：

```csharp
// 新建：submodules/Trident.Net/src/TridentCore.Abstractions/Tasks/TrackerProgress.cs
namespace TridentCore.Abstractions.Tasks;

public abstract record TrackerProgress
{
    private TrackerProgress() { }

    /// <summary>无进度概念（Launch 进程运行中，根本不是「朝终点推进」的任务）</summary>
    public sealed record None : TrackerProgress;

    /// <summary>有进度条但脉冲（CheckArtifact/BuildArtifact 等不可量化阶段）</summary>
    public sealed record Indeterminate(string? Stage) : TrackerProgress;

    /// <summary>精确百分比 0.0–1.0（SolidifyManifest 下载、ResolvePackage 解析等）</summary>
    public sealed record Determinate(string? Stage, double Percent) : TrackerProgress;
}
```

关键洞察：**进度条只属于「朝终点推进」的任务**。Running（游戏在跑）不是推进，用 `None` 表达 → 消费方 `Progress is None` 时天然不渲染进度条，改用运行指示器（●）。

### InstanceState 上移

`InstanceState` 当前在 `TridentCore.Core`，而 `TrackerBase` 在 `TridentCore.Abstractions`（Abstractions 不引用 Core）。归一化下沉要求 TrackerBase 能返回 InstanceState，因此：

```csharp
// 移动：TridentCore.Core/InstanceState.cs → TridentCore.Abstractions/InstanceState.cs
namespace TridentCore.Abstractions;   // 命名空间从 TridentCore.Core 改为 TridentCore.Abstractions

public enum InstanceState { Idle, Installing, Updating, Deploying, Running }
```

全仓 `using TridentCore.Core;` 中引用 InstanceState 的处需更新为 `using TridentCore.Abstractions;`。

### TrackerBase 契约扩展

```csharp
// 修改：TridentCore.Abstractions/Tasks/TrackerBase.cs
public abstract class TrackerBase(...)
{
    // 已有：Key, Token, State, FailureReason, StartedAt, StateUpdated, Abort, Start

    public abstract InstanceState Kind { get; }                    // 新增：子类 override

    public TrackerProgress Progress { get; private set; }          // 新增：统一进度
        = new TrackerProgress.Indeterminate(null);                 // 默认 indeterminate

    private readonly Subject<TrackerProgress> _progressSubject = new();
    public IObservable<TrackerProgress> ProgressChanged => _progressSubject;   // 新增：归一化流

    protected void ReportProgress(TrackerProgress progress)
    {
        Progress = progress;
        _progressSubject.OnNext(progress);
    }

    // Dispose 时 _progressSubject.Dispose()
}
```

### 4 个 Tracker 子类的归一化职责

| Tracker | `Kind` | 进度归一化 | `ReportProgress` 调用点 |
|---------|--------|-----------|------------------------|
| **InstallTracker** | `Installing` | 原生 `Subject<double?>` → 统一 | null → `Indeterminate(null)`；有值 v → `Determinate(null, Normalize(v))` |
| **UpdateTracker** | `Updating` | 同 Install | 同上 |
| **DeployTracker** | `Deploying` | `StageStream` + `ProgressStream<(int,int)>` → 统一 | Stage 切换 → `Indeterminate(stageName)`；(cur,total) → `Determinate(stageName, cur/total)` |
| **LaunchTracker** | `Running` | 无原生进度流，用生命周期 | Start 时 → `Indeterminate("Launching")`；`ProcessAssigned` → `None` |

> **单位统一为 0.0–1.0**。Install/Update 的 `Subject<double?>` 原始单位实施时核实（疑似 0–100），在子类 ReportProgress 时归一化为 0–1。UI 展示用 Scale+Clamp Converter 映射到 ProgressBar。
>
> **Deploy 的 8 个 Stage 只有 2 个有真实 Progress**（ResolvePackage、SolidifyManifest），其余 6 个 `Indeterminate`。这是 Progress 必须三态的硬约束。

子类保留原生 `ProgressStream`/`StageStream`（public 不动，过渡兼容），同时新增统一 `ReportProgress` 输出。Phase 3 消费方全部迁移后，原生流是否降级为 internal 留作 cleanup。

### Snapshot（App 侧，扁平）

```csharp
// 新建：src/Polymerium.Avalonia/Models/InstanceStateSnapshot.cs
public sealed record InstanceStateSnapshot(
    string Key,
    InstanceState State,            // 复用 Trident 枚举
    TrackerProgress Progress,       // None / Indeterminate / Determinate —— 扁平，无嵌套
    TrackerBase? Tracker);          // 钻取引用：Dashboard 取 Process、HomePage 取 DeployStage
```

### InstanceStateAggregator（无状态路由，具体类）

**设计要点**：Aggregator 是「订阅 + 解析 + 路由」的薄层，**不持冗余状态表**。状态唯一真源是 `InstanceManager._trackers`（active tracker 字典），Aggregator 不再拷贝一份。Snapshot 由「事件 + 实时查 IsTracking」即时合成。实体服务不抽接口，直接具体类。

```csharp
// 新建：src/Polymerium.Avalonia/Services/InstanceStateAggregator.cs
public class InstanceStateAggregator
{
    /// <summary>全量变化流（QuickBar Active 区、Sink 用）。基于 DynamicData 的 changeSet。</summary>
    public IObservable<IChangeSet<InstanceStateSnapshot, string>> StateChangeStream { get; }

    /// <summary>单实例流（InstancePageBase 用）。Subscribe 即自动收到当前态，无需先查询。</summary>
    public IObservable<InstanceStateSnapshot?> Watch(string key) { ... }
}
```

> **不暴露 `Get(key)`、不抽接口**。迟来订阅者的「立即拿当前态」由 `Watch` 内部用 `Observable.Defer` + `Replay(1).RefCount()` 解决：每个新订阅者首次 Subscribe 时，`Defer` 实时查 `InstanceManager.IsTracking(key)` 立即吐当前 snapshot；`Replay(1).RefCount()` 让「后到的订阅者」直接复用缓存、不再重查。

**内部行为**：
- 构造时订阅 `InstanceManager` 4 个事件（应用级单例，生命周期=应用）。
- 收到事件 → 把 tracker 包成 snapshot 推进 `StateChangeStream`（用 `ObservableChangeSet.Create` 发 `IChangeSet`，**不持 SourceCache**，状态即时合成）。
- `tracker.ProgressChanged.Buffer(1s)`（节流，沿用现状策略）→ 发 Update change。
- `tracker.StateUpdated`（Finished/Faulted）→ 发 Remove change。
- `Watch(key)` 内部：`Observable.Defer(() => Return(IsTracking ? snapshot : null)).Concat(keyFilteredChanges).Replay(1).RefCount()`。
- Startup 注册 `AddSingleton<InstanceStateAggregator>()`。

### 消费方变薄示例

```csharp
// QuickBar VM（取代 MainWindowContext 365 行）
// Pinned 区 = PersistenceService(pinned keys) × ProfileManager(basic)，overlay Aggregator 状态
// Active 区 = aggregator.StateChangeStream 全集 − Pinned keys
aggregator.StateChangeStream
    .Filter(s => !pinnedKeys.Contains(s.Key))   // Active 不重复 Pinned
    .Sort(SortExpressionComparer<InstanceStateSnapshot>.Descending(s => s.Tracker!.StartedAt))
    .Bind(out _activeView).Subscribe();

// InstancePageModelBase（取代 3 处重复订阅）
aggregator.Watch(Basic.Key).Subscribe(s => State = s?.State ?? InstanceState.Idle);
```

---

## 任务分解

### Phase 0：枚举统一 ⏳ 待实施

**目标**：消灭双枚举，全仓统一为 `InstanceState`。

**做法**：
1. `TridentCore.Core/InstanceState.cs` → 移到 `TridentCore.Abstractions/InstanceState.cs`，命名空间改为 `TridentCore.Abstractions`。
2. 删除 `src/Polymerium.Avalonia/Models/InstanceEntryState.cs`。
3. 改引用：
   - `Models/InstanceEntryModel.cs`：`State` 类型 `InstanceEntryState` → `InstanceState`。
   - `Controls/InstanceEntryButton.cs`：`StateProperty` / `State` 类型 → `InstanceState`。
   - `Controls/InstanceEntryButton.axaml`：样式选择器 `State=Preparing` → `State=Deploying`；元素 `PreparingTag` 改名 `DeployingTag`（可选，为一致性）。
   - `MainWindowContext.cs` 16 处：`InstanceEntryState.X` → `InstanceState.X`（注意 `Preparing` → `Deploying`）。**机械替换保持编译**，这些代码 Phase 3 会整段删除。
   - 本地化 key `InstanceEntryButton_PreparingTagText` → `DeployingTagText`（改 resx + Designer.cs，低优先）。

**验证**：`dotnet build Polymerium.slnx` 通过。

### Phase 1：Tracker 归一化下沉（Trident 侧）⏳ 待实施

**目标**：TrackerBase 原生暴露统一契约，4 个子类内部归一化。

**改动清单**：

| 文件 | 改动 |
|------|------|
| `Abstractions/Tasks/TrackerProgress.cs` | **新建**：record 三层 |
| `Abstractions/Tasks/TrackerBase.cs` | 加 `abstract Kind`、`Progress`、`ProgressChanged`、`protected ReportProgress`；Dispose 清理 Subject |
| `Core/Services/Instances/InstallTracker.cs` | `override Kind => Installing`；订阅自身 `ProgressStream` → 归一化 `ReportProgress`（单位→0–1） |
| `Core/Services/Instances/UpdateTracker.cs` | 同 Install，`Kind => Updating` |
| `Core/Services/Instances/DeployTracker.cs` | `Kind => Deploying`；订阅 `StageStream` → `Indeterminate(stage)`；订阅 `ProgressStream<(int,int)>` → `Determinate(stage, cur/total)` |
| `Core/Services/Instances/LaunchTracker.cs` | `Kind => Running`；Start 时 `ReportProgress(Indeterminate("Launching"))`；`ProcessAssigned` 里 `ReportProgress(None)` |
| `Core/Services/InstanceManager.cs` | deploy 循环补 `EnsureRuntime` 的 Stage case（当前缺失）；核实 Install/Update 下载器喂流单位 |

**验证**：Trident 编译通过 + App 编译通过（双轨期，原生流与统一流并存）。

### Phase 2：InstanceStateAggregator（App 侧新服务）⏳ 待实施

**目标**：新服务落地，成为 InstanceManager 事件的唯一聚合者（旧订阅暂留双轨）。Aggregator 是无状态路由器，不持冗余状态表。

**改动清单**：

| 文件 | 改动 |
|------|------|
| `Models/InstanceStateSnapshot.cs` | **新建**：扁平 record |
| `Services/InstanceStateAggregator.cs` | **新建**：具体类（不抽接口）。注入 `InstanceManager`，构造即订阅 4 事件；用 `ObservableChangeSet.Create` 推 changeSet（无 SourceCache）；`ProgressChanged.Buffer(1s)` 节流；`StateUpdated` Finished/Faulted → Remove；`Watch(key)` = `Defer(IsTracking 查询) + Replay(1).RefCount()` |
| `Startup.cs` | `AddSingleton<InstanceStateAggregator>()`；删除 `StateService` 空壳 |
| `Services/StateService.cs` | **删除**（空壳占位，被 Aggregator 取代） |

**验证**：编译通过；可写临时诊断页/日志确认事件正确聚合（旧订阅未动，行为不变）。

### Phase 3：消费方迁移（删重复订阅 + Sink 落地）⏳ 待实施

**目标**：3-4 处重复订阅全部切到 Aggregator；MainWindowContext 里的非状态副作用（通知/活动记录/崩溃诊断）迁进独立 Sink，**不再塞进 Aggregator**。Aggregator 保持纯路由。

**Sink 架构（窗口存活感知）**：

当前 `NotificationService.Pop()` 已是双 handler 模式：`_notificationHandler`（持久通知记录）+ `_growlHandler`（一次性弹窗）。两者都被绑死在主窗口，窗口没了就一起没。Phase 3 重构：

| Sink | 副作用 | UI 绑定？ | 无窗口时 |
|------|--------|----------|---------|
| **ActivitySink** | 写 PersistenceService 活动记录 | ❌ 纯数据 | 永远执行 |
| **NotificationSink** | 通知记录（持久集合）+ growl 弹窗 | 记录❌ / 弹窗✅ | 记录永远写；growl 走网关 |
| **CrashDiagnosisSink** | 崩溃诊断（Danger growl + Diagnose 按钮） | ✅ | 走网关 |

**关键重构**：
1. **持久通知集合从 `MainWindowContext` 上移到 app 级**。`NotificationService` 自己持有 `ObservableCollection<NotificationModel>`（app 级单例，天然存活），`NotificationSidebar` 绑这个。`_notificationHandler` 永远写它，不受窗口影响。
2. **growl handler 套 `IInteractiveSurface` 网关**：实时查「有没有活跃 TopLevel」（同 `TopLevelHelper` 模式），有就派发到当前窗口 GrowlHost。
3. **无窗口策略 = B（OS 通知）**，但**当前留空 + TODO**：
   ```csharp
   // TODO(B): 无窗口时通过 TrayIcon / macOS Notification Center 发系统通知
   // 现状：growl 静默丢弃（持久记录照写），崩溃诊断也转持久记录
   ```
   **UX 原则**：无窗口时**绝不**实例化主窗体弹 modal——这不符合 macOS 习惯（用户期望「关窗=我待会再看」，软件不夺焦）。正确链路是 OS 通知 + Dock 角标 + 持久记录，等用户主动重开窗口。等 POLY-99 后做托盘/系统通知再实现 B。

**改动清单**：

| 文件 | 改动 |
|------|------|
| `Services/Sinks/ActivitySink.cs` | **新建**：订阅 Aggregator，tracker Finished/Faulted 时写 PersistenceService 活动记录 |
| `Services/Sinks/NotificationSink.cs` | **新建**：订阅 Aggregator，按 Kind/State 映射通知文案 + 触发 `NotificationService` 双 handler |
| `Services/Sinks/CrashDiagnosisSink.cs` | **新建**：订阅 Aggregator，Running Faulted 且为 `ProcessFaultedException` 时发崩溃诊断通知 |
| `Services/NotificationService.cs` | 持久 `ObservableCollection<NotificationModel>` 上移为 app 级状态；`_growlHandler` 套网关，无窗口策略 TODO(B) |
| `MainWindowContext.cs` | 删除 `SubscribeState`（约 365 行）+ 4 个 `OnInstance*` 处理器 + 进度订阅 + 副作用调用；改为订阅 `aggregator.StateChangeStream` 更新 `_entries` 中对应 `InstanceEntryModel` 的 `State`/`Progress`。**`SubscribeProfileList`（列表增删）暂保留**——整体搬走属于 MAINWINDOW-DECOUPLING Step 3 |
| `PageModels/InstancePageModelBase.cs` | 删除 3 处 `InstanceManager.*` 订阅 + `IsTracking` 初始查询 + `OnInstance*StateChanged`；改为 `aggregator.Watch(Basic.Key).Subscribe(...)`。保留 virtual hooks |
| `PageModels/InstancePageModel.cs` | 删除重复订阅区（含那句 `// 终究还是得有个 InstanceStateAggregator` 注释），复用基类 |
| `PageModels/InstanceHomePageModel.cs` | `OnInstanceDeploying` override 改为从 `snapshot.Tracker as DeployTracker` 取 `CurrentStage`/进度细节 |
| `PageModels/InstanceDashboardPageModel.cs` | `OnInstanceLaunching` override 改为从 `snapshot.Tracker as LaunchTracker` 取 `Process` 启动监控 |

**验证**：编译通过；手动验证安装/更新/部署/启动四条路径的状态展示、进度、通知、活动记录、崩溃诊断均正常。

### Phase 4：下游衔接（QuickBar / Sidebar / POLY-23 主体）⏳ 待实施

本 Phase 是 POLY-23 的 UI 部分，依赖 Phase 2+3 完成，独立成节：

- **Toolbar → QuickBar 改名**：`MainWindow.axaml` 的 Toolbar 区 + 相关资源 key。
- **QuickBar 加实例区**：Pinned 区（PersistenceService 存 pinned keys × ProfileManager basic，overlay Aggregator 状态）+ Active 区（`aggregator.StateChangeStream` − Pinned keys），共用 `InstanceEntryButton`。
- **InstanceSidebar**：纯入口视图，数据源 ProfileManager 列表 + 分类 chip（Loader/Source/Tag）+ 搜索，**过滤掉安装中实例**（安装完 ProfileAdded 才 AddModel）。从 Accounts 按钮原位置触发 `OverlayService.PopSidebar()`。
- **Sidebar 走 Activator + SidebarModel**（对齐已有基础设施）：当前 `OverlayService.PopSidebar` 不走 `IViewActivator`（`new NotificationSidebar()` 无参 ctor 无法注入），而 `PopModal` 走 activator。改为：参照已有的 Modal 模式，新增 `SidebarModels/` 目录（对齐 `ModalModels/`），`SidebarModel` 继承 `ViewModelBase`（对齐 `SnapshotsModalModel` 等）。`OverlayService` 加 `PopSidebar<TSidebar>()` 走 activator，对齐 `PopModal<TModal>()`。新建的 `InstanceSidebar` + `InstanceSidebarModel` 直接用新机制；旧的 `NotificationSidebar` 一并改造注入 `NotificationService`、绑其 app 级持久集合（替代当前通过 `$parent[app:MainWindow]` 反向找 MainWindowContext 的写法）。
- **Accounts 入口移位**：按 POLY-23 描述挪到右侧。

> Pinned 存储用 `PersistenceService` 新增实例级泛型存取（参照现有 `GetWidgetLocalData<T>/SetWidgetLocalData<T>`）。

---

## Pinned / Active 显示逻辑（确认）

```
Aggregator 内部表：只持有「非 Idle」实例 snapshot

Pinned 区 = PersistenceService(pinned keys) × ProfileManager(basic)
            overlay Aggregator 状态（有快照显活跃态，无则 Idle）
            → 永远在前面，静态列表 + 状态点

Active 区 = Aggregator.StateChangeStream 全集  −  Pinned keys
            → 动态进出；安装中实例天然落此（未落盘不可 pin）
            → 目的：看有状态的实例

规则：
  Running + Pinned  → 只在 Pinned 区（Active 不重复）
  Running + 非 Pinned → Active 区
  Installing（任何）→ Active 区（未落盘，不进 Sidebar/Pinned）
  Pinned + Idle     → Pinned 区（Idle，不亮）
  非 Pinned + Idle  → 都不在（去 Sidebar 找）
```

---

## 风险与边界

1. **Trident 子模块改动 blast radius**：Phase 1 改 TrackerBase + 4 子类 + InstanceManager，影响所有 tracker 消费方。但本质是**统一**而非破坏——原生流 public 保留（双轨过渡），消费方逐步迁移。每个 Phase 独立编译验证、独立提交。
2. **InstanceState 上移波及全仓 using**：机械替换，风险低但范围广，需一次提交完成。
3. **Aggregator 无状态路由 vs changeSet 的张力**：`StateChangeStream` 用 `ObservableChangeSet.Create` 即时合成，状态真源仍是 `InstanceManager._trackers`。**不引入 SourceCache 冗余表**。`Watch(key)` 用 `Defer + Replay(1).RefCount()` 兼顾「迟来订阅者立即拿当前态」和「refcount=0 后重订阅」两种情况，订阅者无需先 `Get`。
4. **无窗口副作用策略（B，待实现）**：macOS 关窗不退出后，「后台出错」应走 OS 通知 + Dock 角标 + 持久记录，**绝不**实例化主窗体弹 modal（违反 macOS 习惯）。当前 Phase 3 的 gateway 留 TODO(B)，行为：持久记录照写、growl/诊断静默。等 POLY-99 后做托盘/系统通知时实现。
5. **Install/Update 进度单位**：实施时核实 `Subject<double?>` 原始单位（0–100 or 0–1），在子类 ReportProgress 处归一化为 0–1。
6. **EnsureRuntime 死 case**：当前 deploy 循环 switch（`InstanceManager.cs:263-298`）无 EnsureRuntime case，既不喂 Stage 也不喂 Progress。Phase 1 补 case 或确认其已被合并/删除。

---

## 进度速览

| 阶段 | 内容 | 状态 |
|------|------|------|
| Phase 0 | 枚举统一（InstanceState 上移 + 废弃 InstanceEntryState） | ⏳ 待实施 |
| Phase 1 | Tracker 归一化下沉（Trident 侧） | ⏳ 待实施 |
| Phase 2 | InstanceStateAggregator 新服务 | ⏳ 待实施 |
| Phase 3 | 消费方迁移（删 400+ 行重复订阅） | ⏳ 待实施 |
| Phase 4 | QuickBar / Sidebar / POLY-23 UI | ⏳ 待实施（依赖 2+3） |

**下一步行动**：确认本计划后，从 Phase 0 开始，每阶段独立编译验证、独立提交。
