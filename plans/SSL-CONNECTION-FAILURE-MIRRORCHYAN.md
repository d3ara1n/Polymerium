# POLYMERIUM-21: SSL Connection Failure in MirrorChyan Update Check

> **Sentry**: https://gravitylab.sentry.io/issues/POLYMERIUM-21
> **Status**: unresolved (new)
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

### D. 标记为 ignored
如果只发生一次且不再复现，说明是用户侧网络问题，可以 ignored。

---

## Decision

暂缓，日后排查 Velopack MirrorChyan 扩展中 Task observe 的问题。当前只发生 1 次。
