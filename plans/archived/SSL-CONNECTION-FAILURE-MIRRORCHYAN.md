# POLYMERIUM-21: SSL Connection Failure in MirrorChyan Update Check

> **Sentry**: https://gravitylab.sentry.io/issues/POLYMERIUM-21
> **Status**: resolved (code-side mitigation 2026-06-13)
> **Date**: 2026-06-09
> **Release**: 1.9.0

---

## Problem

更新检查时，`MirrorChyanService.GetLatestVersionAsync` 的 SSL 握手失败，抛出 `UnobservedTaskException`：

```
System.AggregateException: A Task's exception(s) were not observed...
→ System.Net.Http.HttpRequestException: The SSL connection could not be established
  → System.Security.Authentication.AuthenticationException: Cannot determine the frame size or a corrupted frame was received.
```

调用链：
```
UpdateService.StartAsync
  → updateManager.CheckForUpdatesAsync()
    → Velopack MirrorChyan Source
      → Refit IMirrorChyanClient.GetLatestVersionAsync()
        → HttpClient.SendAsync()
          → SslStream.AuthenticateAsClientAsync() ← 失败
```

关键标签：
- `mechanism: UnobservedTaskException` — Task 未被 await/observe，由 finalizer 线程重新抛出
- `handled: no`
- 用户：Tony，US/Seattle，Windows 10，zh-CN locale

---

## Analysis

1. **网络层面**：底层 SSL 错误「Cannot determine the frame size or a corrupted frame was received」通常意味着 TLS 握手时收到了非预期的数据——可能是代理干扰、防火墙注入、或不稳定的网络连接。这是用户的网络环境问题，不是 Polymerium 的 bug。

2. **Task 未被 observe**：`UnobservedTaskException` 说明某个 `Task` 在 fire-and-forget 场景中被创建，没有 await。发生 SSL 错误后无人 catch，最终由 GC finalizer 重新抛出。可能是 Velopack MirrorChyan 扩展内部或 Refit 生成的代码没有正确 observe 任务。

3. **已有兜底**：`UpdateService.StartAsync` 中已有 try-catch 静默吞异常，但这个 error 绕过了这层保护，因为它不是直接在这个调用栈上抛出的。

---

## Potential Fixes

### A. 调查 Velopack MirrorChyan 扩展
检查 Velopack extension（可能在 NuGet 包或 submodule 中）是否在某个地方创建了未被 observe 的 Task。

### B. 全局 UnobservedTaskException 处理
在 App 启动时注册 `TaskScheduler.UnobservedTaskException` 事件处理，防止未观察的异常导致 sentry 上报。

### C. 网络重试 / 更优雅的错误处理
对 MirrorChyan 的 HTTP 调用增加更健壮的重试逻辑或降级策略。

> **已排除**：调查确认 Polymerium 的 `Startup.cs` 已通过 `ConfigureHttpClientDefaults` + `AddTransientHttpErrorPolicy` 为所有经 `IHttpClientFactory` 创建的 client（包括 MirrorChyan.Net 注册的 Refit client）配置了 3 次指数退避重试。而复现场景（用户开启 VPN/梯子）属于确定性网络失败——TLS 握手被同一中间人持续破坏，重试必然全部失败且令用户多等 ~14s，反而有害。故本项否决。

### D. 标记为 ignored
如果只发生一次且不再复现，说明是用户侧网络问题，可以 ignored。

---

## 调查结论（2026-06-13）

逐行复核 `VelopackExtension.MirrorChyan` 与 `MirrorChyan.Net` 两个仓库的调用链后确认：
- `MirrorChyanSource.GetReleaseFeed`、`MirrorChyanService.GetLatestVersionAsync` 全部为干净 `await` + `ConfigureAwait(false)`，**无 fire-and-forget**；
- 该链路本身已由 Polymerium 全局 Polly 重试保护（见上 C 项结论）；
- 因此孤儿 Task 不源自这条链路，最大嫌疑是 Velopack `UpdateManager.CheckForUpdatesAsync()` 内部（无源码，未坐实）。

原 Decision 中「排查 MirrorChyan 扩展的 Task observe 问题」已排除——扩展本身无 observe 缺陷。

## Decision

代码侧已处理（2026-06-13）。

在 `Polymerium.Avalonia/App.axaml.cs` 的 `TaskScheduler.UnobservedTaskException` handler 中增加网络类异常的分级处理：
- 沿异常链（含 `AggregateException` / `InnerException`，带循环引用保护）判断是否含 `HttpRequestException` / `SocketException` / `AuthenticationException` / `IOException`；
- 命中则 `e.SetObserved()` **阻止进程崩溃**，但仍以 `SentryLevel.Warning` 上报 Sentry 并打 tag `polymerium.source = NetworkUnobserved`，**保留对真正网络代码错误的可观测性**；
- 非网络类异常维持原 `ErrorReporter.Report`（critical）路径不变。

这样用户开启梯子/代理导致 TLS 握手失败时不再崩溃、不再污染 Sentry 关键流，而真正的网络代码错误仍可按该 source / level 筛出排查。

Sentry issue POLYMERIUM-21 可在 Web 端 resolve/ignore，代码侧已不会让其再进入关键流。
