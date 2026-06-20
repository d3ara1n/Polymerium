namespace Polymerium.Avalonia.Models;

/// <summary>
///     详情面板依赖表中的一条依赖项：选中包所依赖的某个已安装包，标记是否强制。
/// </summary>
public sealed record DependencyEntry(
    string Key,
    string ProjectName,
    bool IsRequired
);
