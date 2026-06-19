namespace Polymerium.Avalonia.Models;

/// <summary>
///     依赖图节点。图中只包含当前实例已安装的包，节点身份由 <see cref="Key" /> 唯一确定
/// （Label + Namespace + ProjectId）。AvaloniaGraphControl 的 GraphPanel 按引用比较节点，
/// 因此相同 Key 必须复用同一实例。
/// </summary>
/// <param name="Key">节点唯一标识，用于去重与字典查找</param>
/// <param name="Label">节点显示文本（ProjectName）</param>
public sealed record DependencyGraphNode(string Key, string Label);
