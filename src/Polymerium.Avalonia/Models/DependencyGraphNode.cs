namespace Polymerium.Avalonia.Models;

/// <summary>
///     依赖图节点模型（POC 阶段）。
///     阶段 3 接入 ProfileManager 真实数据时，会演化为携带 ProjectId / Version 的正式模型。
/// </summary>
public sealed record DependencyGraphNode(string Label);
