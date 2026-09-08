# Fire-and-forget 任务异常观察

## 背景

Polymerium 接入 Sentry 后，线上有相当一部分事件没有可用调用栈：issue POLYMERIUM-1K（NullReferenceException，314 次命中）以 UnobservedTaskException 形态从 finalizer 线程重抛——outer 栈丢失是 .NET 机制，事后手段（符号、事件增强）均无法恢复，必须在任务被丢弃的那一侧完成观察。根因形态是散布在 PageModel 里的 `_ = XxxAsync()` 式任务丢弃。

## 定案的方案

全库清点后放弃了两条预先设想的路线：SafeFireAndForget 扩展方法、集中式后台任务队列，也不挂 Dispatcher.UnhandledException 钩子。理由：显式 catch 后的异常不应再作为 Unhandled 上报，上报的价值只在未被 handle 的异常上；真正的缺口不是上报，而是异常流可见、原地能修复。据此确立的纪律：

- **预期内异常原地 catch 并处理**（网络错误 toast、IO 失败走导入失败路径、探测失败保持空态、预取失败静默待重试），且只 catch 具体类型，不写万能 `catch (Exception ex) report`。
- **预期外异常就该崩**：让异常带完整原始栈抛到 UI 线程（async void / await 链），走 AppDomain.UnhandledException 上报，进程终止。意料之外的异常是 bug，崩掉是正确形态。
- **`async void` 在事件处理器语境合法**：属性变化回调（PropertyChanged 语义）、生命周期事件（Loaded/Closing/ShutdownRequested）均属此类。

## 实施明细

消灭的丢弃点（全部改为 await 形态）：

- ExplorerPageModel：`OnInitializeAsync` 外的 4 处搜索触发（Post lambda async 化、`OnIsFilterEnabledChanged` / `OnSelectedKindChanged` async void 化、`FavoritePackageAsync` 内直接 await）。
- MarketplaceModpacksPageModel：`OnInitializeAsync` 改并行发起 + 末尾 await，`OnSelectedRepositoryChanged` / `OnFilteredVersionChanged` / `OnFilteredLoaderChanged` async void 化。
- App.axaml.cs：退出确认两处（`OnShutdownRequested` async void 化、`window.Closing` lambda async 化），`RunExitConfirmationAsync` 本体保持 try/finally 不加 catch——弹框与沉降逻辑没有预期内异常面，静默失灵比崩溃更糟。
- AccountCreationMicrosoft：Retry 命令改为真正的异步命令（返回 Task）。

原地救援：

- InstancePageModel.ImportFromFileAsync（拖放导入）：确认导入段包 `catch when (ex is IOException or UnauthorizedAccessException)`，走 `PopMessage(ex, ...)` 失败 toast + 新增 `InstancePage_ImportDangerNotificationTitle` 本地化键。其余异常照崩。

判定安全、不动的丢弃点（被调用方内部已收枕，Task 不可能 fault）：

- Trident InstanceManager 的 5 处（`ExecuteAsync` 全量收枕为 Faulted 终态 + 日志）。
- `StartLifetimeServicesAsync`（内部 catch → Fatal 上报）、`MonitorAsync`（循环内全量 catch）、`LoadGalleryThumbnailsAsync`（per-item catch）、`PrefetchAsync`（显式静默重试语义）、`LoadModelAsync`（catch → ErrorMessage）、`CaptureHomeAsync`（`ProbeHomeAsync` 内部 catch-all → null，探测失败即空态展示，`?` / Unknown 占位是既有的失败可视层）。

## 结果

全库无"未观察的静默失败"：每个丢弃点要么已消灭，要么经分析确认 Task 不可能 fault。搜索/筛选/退出链路上的预期外异常从 UnobservedTaskException 丢栈形态变为带完整栈的崩溃上报，POLYMERIUM-1K 的事件形态（finalizer 重抛、仅剩一帧）从源头消除。附带发现并修正的既有缺口：拖放导入的 IO 失败此前完全静默。

一个 C# 语法坑：partial 方法实现带 async 时修饰符必须写 `async partial void`（async 在前），`partial async void` 报 CS1002 解析错误。
