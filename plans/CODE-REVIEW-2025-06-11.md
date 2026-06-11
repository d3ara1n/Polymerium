# Code Review — 2025-06-11

Build status: ✅ **succeeded** (0 warnings, 0 errors)

Scope: `src/Polymerium.App/` + `submodules/Trident.Net/`

---

## 🔴 Critical

### 1. Service Locator Anti-Pattern

- **File:** `src/Polymerium.App/Program.cs:35`
- **Detail:** `internal static IServiceProvider? Services` 将 DI 容器全局暴露。至少 10 处调用点直接通过 `Program.Services` 解析服务（`App.axaml.cs:82,114,147,155,162,164`、`Program.cs:93,98,115,136`）。
- **风险:** 隐藏依赖关系、失去编译期验证、生命期不匹配不可见。
- **建议:** 通过构造函数/方法参数传递所需服务。对于 Avalonia XAML 构造边界，可用 `ActivatorUtilities.CreateInstance` 替代原始查找。

### 2. AppImageLoader Never Disposed

- **File:** `src/Polymerium.App/Program.cs:98-100`
- **Detail:** `new AppImageLoader(...)` 创建后赋值给静态 `AsyncImageLoader`，但关闭序列中从未调用 `Dispose()`。`AppImageLoader` 持有 `MemoryCache` 和 `BaseWebImageLoader`。
- **风险:** 内存泄漏、HttpClient 泄漏。
- **建议:** 在 DI 中注册为 Singleton，或在关闭区段显式 `.Dispose()`。

### 3. Fire-and-Forget StartLifetimeServicesAsync

- **File:** `src/Polymerium.App/App.axaml.cs:68,91`
- **Detail:** `_ = StartLifetimeServicesAsync(desktop)` 丢弃 `Task`。catch 中 `desktop.Shutdown()` 在后台线程调用，违反 Avalonia 线程模型。
- **风险:** 异常逃逸后 App 可能无响应。
- **建议:** 在 catch 内使用 `Dispatcher.UIThread.Post(() => desktop.Shutdown(-1))`。

### 4. Configuration 线程不安全单例

- **File:** `src/Polymerium.App/Configuration.cs` + `Services/ConfigurationService.cs`
- **Detail:** `Configuration` 是可变的 POCO（~30 个 setter 属性），注册为 Singleton。被 UI 线程和后台服务（UpdateService、HttpClient 等）同时读写。
- **风险:** 数据竞争，读取到部分修改的状态。
- **建议:** 改为 immutability + `with` 表达式，或加锁保护，或 copy-on-read 快照语义。

### 5. TrackerBase 线程不安全状态机

- **File:** `submodules/Trident.Net/src/TridentCore.Abstractions/Tasks/TrackerBase.cs:42-60`
- **Detail:** `OnStart()` 同步设置 `State`，然后 fire-and-forget `RunCoreAsync()`。`State` 读写无同步。多次 `Start()` 可竞争。
- **风险:** 状态不一致、StateUpdated 在不同线程并发触发。
- **建议:** `Start()` 加 idempotent guard、用 `Interlocked.CompareExchange` 做状态转换、确保回调线程安全。

### 6. PathDef.Default 可变静态单例

- **File:** `submodules/Trident.Net/src/TridentCore.Abstractions/PathDef.cs:5`
- **Detail:** `public static PathDef Default { get; set; }` — 任何代码可在任何时候替换它，并发首次访问 + 替换存在竞争。
- **风险:** 中途 swap 导致不一致的文件路径。
- **建议:** 用 `Lazy<PathDef>` 或 immutability singleton，setter 设为 private。

### 7. ImporterAgent.ExtractFilesAsync 资源泄漏

- **File:** `submodules/Trident.Net/src/TridentCore.Core/Services/ImporterAgent.cs:44-52`
- **Detail:** 手动 `.Close()` 而不使用 `using`。若 `CopyToAsync` 抛异常，流不释放。
- **风险:** 文件句柄泄漏。
- **建议:** 替换为 `await using var fromStream = ...` 和 `await using var file = ...`。

### 8. ProfileManager.Add 同步 .Wait() + 重复 SaveAsync

- **File:** `submodules/Trident.Net/src/TridentCore.Core/Services/ProfileManager.cs:105-108`
- **Detail:** `SaveAsync()` 被调用了两次（一次在 `_profiles.Add` 前，一次在后），且使用 `.Wait()`。
- **风险:** 死锁风险 + 重复 I/O。
- **建议:** 改为 async、删除第一次重复 save。

---

## 🟠 High

### 9. 关闭时同步 .GetAwaiter().GetResult()

- **File:** `src/Polymerium.App/Program.cs:115-120`
- **Detail:** `runtime.StopAsync().GetAwaiter().GetResult()` 阻塞主线程。
- **风险:** 若任何 ILifetimeService 有同步上下文依赖则死锁。
- **建议:** 加 timeout `WaitAsync(TimeSpan.FromSeconds(10))`。

### 10. Singleton 服务的 Handler Delegate 非线程安全

- **File:** `Services/NavigationService.cs:30-35`, `OverlayService.cs:18-24`, `NotificationService.cs:35-36`
- **Detail:** `SetHandler(...)` 写入字段，之后在 UI 线程读取。无 `volatile`、无锁。
- **风险:** 潜在数据竞争，虽实践中启动时设置一次。
- **建议:** 标记为 `volatile` 或加 `Debug.Assert(Dispatcher.UIThread.CheckAccess())`。

### 11. MainWindowContext 事件订阅永不取消

- **File:** `src/Polymerium.App/MainWindowContext.cs:68-69,87`
- **Detail:** `SubscribeProfileList` 和 `SubscribeState` 订阅的 `ProfileManager` / `InstanceManager` 事件，在 `OnDeinitialize()` 中不取消订阅。
- **风险:** 如果 Context 被重建，事件处理器会累积。
- **建议:** 在 `OnDeinitialize` 中存储并取消事件处理器。

### 12. ScrapService / WidgetHostService 非线程安全 Dictionary

- **File:** `Services/ScrapService.cs:20`, `Services/WidgetHostService.cs:18-19`
- **Detail:** 用普通 `Dictionary` 在可能并发的场景下读写。
- **风险:** 并发写/读导致崩溃。
- **建议:** 替换为 `ConcurrentDictionary`。

### 13. ConfigurationService.Dispose 静默吞写错误

- **File:** `src/Polymerium.App/Services/ConfigurationService.cs:63-71`
- **Detail:** `Dispose()` 中 catch 所有异常并静默忽略，是唯一写入配置的地方。
- **风险:** 若写入失败（盘满、权限），用户静默丢失全部配置。
- **建议:** 至少记录日志。写临时文件后再替换。

### 14. DataService Bitmap 内存泄漏

- **File:** `src/Polymerium.App/Services/DataService.cs:82-101`
- **Detail:** IMemoryCache 驱逐 `Task<Bitmap>` 时不释放 Bitmap。长时间浏览独特图片累积大量非托管内存。
- **风险:** 内存压力累积直到 GC finalizer。
- **建议:** 实现 `PostEvictionCallbacks` 追踪释放状态，或使用 WeakReference。

### 15. Persistence 层 O(n) 物化全表

- **File:** `Services/PersistenceService.cs` + `Services/GarbageCollector.cs`
- **Detail:** `SearchFavoriteProjects`、`GetTotalPlayTimeRank`、`GetLongestSession`、`GarbageCollector` 全部用 `.ToList()` 加载整表再在内存过滤/计算。
- **风险:** 数据量增长后性能严重退化。
- **建议:** 将过滤/聚合推到 SQL 层。

### 16. IRepository 违反接口隔离原则

- **File:** `submodules/Trident.Net/src/TridentCore.Abstractions/Repositories/IRepository.cs`
- **Detail:** 16 个方法混在单一接口：查询、解析、标识检查、健康检查。
- **风险:** Mock/stub 必须实现全部 16 个方法。
- **建议:** 拆分为 `IRepositoryQueryService`、`IRepositoryResolutionService`、`IRepositoryInfoService`。

### 17. CompressedProfilePack.Open NRE via !

- **File:** `submodules/Trident.Net/src/TridentCore.Abstractions/Importers/CompressedProfilePack.cs:25`
- **Detail:** `_archive.GetEntry(fileName)!.Open()` — `GetEntry` 返回 null 时抛出无意义的 NRE。
- **风险:** 无法诊断哪个文件缺失。
- **建议:** Guard: `?? throw new FileNotFoundException(...)`。

### 18. CLI Operations 的 sync-over-async

- **File:** `submodules/Trident.Net/src/TridentCore.Cli/Operations/PackageOperation.cs`、`InstanceOperation.cs`
- **Detail:** `guard.DisposeAsync().AsTask().GetAwaiter().GetResult()` 在同步方法中对异步 dispose 同步阻塞。
- **风险:** 死锁 + 异常掩盖。
- **建议:** 改为 `async Task<>` + `await using`。

---

## 🟡 Medium

| # | Issue | Location |
|---|-------|----------|
| 19 | `UpdateService.IsChecking` race condition — 无同步保护 | `Services/UpdateService.cs:56` |
| 20 | `ErrorReporter.Report` 静默丢弃非严重错误 — 无法在 debug 中诊断 | `ErrorReporter.cs:22-25` |
| 21 | `ErrorReporter.Dump` 重复 `AggregateException` 首个内部异常 | `ErrorReporter.cs:87-93` |
| 22 | `MainWindow.Instance` 静态单例窗口引用 — 关闭后阻止 GC | `MainWindow.axaml.cs:25` |
| 23 | `Program.Terminate` `exitAction` 字段无同步 | `Program.cs:142-150` |
| 24 | `NotificationService.ProgressHandle` double-dispose 竞态 | `Services/NotificationService.cs:178-190` |
| 25 | `InstanceService.DeployAndLaunch` account 为 null 时静默无操作 | `Services/InstanceService.cs:33-91` |
| 26 | `DataService.GetOrCreate` 缓存 faulted Task — 后续命中返回失败任务 | `Services/DataService.cs:183-191` |
| 27 | `AssetModHelper` 大量静默 `catch {}`（~18 处）— 调试困难 | `Utilities/AssetModHelper.cs` |
| 28 | `ModrinthExporter/Importer` 硬编码 URL path slicing — CDN URL 格式变化时脆弱 | `submodules/Trident.Net/.../ModrinthExporter.cs:67-75` |
| 29 | `ProfileManager.Dispose` 结束后设 `_isDisposing = false` — 误导 guard | `submodules/Trident.Net/.../ProfileManager.cs:192` |
| 30 | `McpHost.RunAsync` 即使失败也始终返回 0 | `submodules/Trident.Net/.../McpHost.cs:10-28` |

---

## 🟢 Low

| # | Issue | Location |
|---|-------|----------|
| 31 | `BuildAvaloniaApp()` 是 public — 仅设计器需要 | `Program.cs:157` |
| 32 | 硬编码密钥 `MagicWords` / `MirrorChyanCdk` — 应移入配置 | `Program.cs:19,21` |
| 33 | `TopLevelHelper` 使用废弃的 `TopLevel.Clipboard` API | `Utilities/TopLevelHelper.cs:118` |
| 34 | TridentExporter 中文注释遗留 | `submodules/Trident.Net/.../TridentExporter.cs:16` |
| 35 | `ISnapshotStore` 同步接口处理可能 I/O 的操作 | `submodules/Trident.Net/.../ISnapshotStore.cs` |
| 36 | Exporter/Importer 接口缺少 `CancellationToken` | `submodules/Trident.Net/.../IProfileExporter.cs`、`IProfileImporter.cs` |

---

## 🏗️ 架构观察

1. **God object: MainWindowContext**（~600 行）— 处理导航、实例状态、通知、导出、崩溃报告，建议拆分为多个聚焦的 coordinator。

2. **Handler-passing pattern** — `NavigationService`、`OverlayService`、`NotificationService` 使用 `SetHandler()` 模式，创建时序耦合，建议用工厂或初始化接口。

3. **4 个 Exporter 的离线物料化代码重复** — `CurseForge`、`Modrinth`、`Trident`、`MultiMc` 四个 Exporter 包含几乎相同的离线模式代码，建议提取公共 helper。

4. **SnapshotStore 释放不属于自己的 IFreeSql** — 所有权转移未记录文档。

5. **无 `IAsyncDisposable` 支持** — 关闭序列使用 `GetAwaiter().GetResult()` + `Dispose()` 混合，脆弱。

6. **大量空的 catch** — Utility 层（AssetModHelper 等）广泛使用裸 `catch {}`，产出空结果而不是提示错误。

---

## 优先级建议

| 优先级 | 修复项 |
|--------|--------|
| **立即** | #3 (fire-and-forget Shutdown 线程违规) |
| **本周** | #1 (Service Locator)、#4 (Configuration 线程安全)、#8 (ProfileManager .Wait()) |
| **本次迭代** | #2, #7, #9, #11, #12, #13 |
| **下次迭代** | #5, #6, #10, #14, #15, #16, #18 |
| **待定** | 其余 medium/low + 架构重构 |
