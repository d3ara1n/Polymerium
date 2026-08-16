# Fire-and-forget 任务异常观察

## 背景

Polymerium 已接入 Sentry，线上问题的排查高度依赖调用栈，但相当一部分事件根本没有可用栈。实证：issue [POLYMERIUM-1K](https://gravitylab.sentry.io/issues/POLYMERIUM-1K)（NullReferenceException，314 次命中）以 UnobservedTaskException 形态从 finalizer 线程重抛——outer 栈丢失是 .NET 机制，inner 只剩一帧。根因形态是代码里 `_ = XxxAsync()` 式的任务丢弃，散布在多个 PageModel 的搜索/加载触发点上。CI 符号上传已解决行号问题，但符号救不回丢失的栈，这类事件必须从源头消除。

## 做什么

全库（Polymerium.Avalonia 与 Trident 两侧）清点 fire-and-forget 形态的后台任务，建立统一的异常观察策略，让被丢弃的 Task 抛出的异常不再是无人观察的静默失败。

## 期望效果

后台任务抛异常时，Sentry 事件携带完整的原始调用栈，可归因到具体页面/服务；不再以 UnobservedTaskException 从 finalizer 线程重抛的形态出现。

## 注意事项（调研所得）

- 观察必须在任务被丢弃的那一侧完成——finalizer 重抛时原始栈已被 async 状态机覆盖，事后手段（符号、事件增强）均无法恢复。
- 连续触发导致的重入/竞态是与异常观察正交的另一个问题，不纳入本计划，避免范围蔓延。
- `async void` 在事件处理器里是合法的 Avalonia 模式，不在清剿范围；其他位置的 `async void` 与丢弃 Task 同罪处理。
- 方案空间（实施时对照现状取舍）：SafeFireAndForget 风格的扩展方法统一上报 Sentry/log，或集中式后台任务队列。前者侵入小，后者适合长驻服务型任务，可能并存。
