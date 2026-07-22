namespace Polymerium.Avalonia.Models;

/// <summary>
///     ///     详情面板前置/附属表中的一条条目，标记是否强制依赖以及该包是否缺失（未安装）。
/// </summary>
public sealed record DependencyEntry(string Key, string ProjectName, bool IsRequired, bool IsMissing);
