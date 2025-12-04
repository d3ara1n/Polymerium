using System;
using System.IO;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

/// <summary>
/// 日志数据源模型
/// </summary>
public class LogSourceModel(LogSourceKind kind, string displayName, string? filePath, DateTimeOffset? modifiedAt)
    : ModelBase
{
    /// <summary>
    /// 数据源类型
    /// </summary>
    public LogSourceKind Kind { get; } = kind;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; } = displayName;

    /// <summary>
    /// 文件路径（仅文件类型有值）
    /// </summary>
    public string? FilePath { get; } = filePath;

    /// <summary>
    /// 文件修改时间（仅文件类型有值）
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; } = modifiedAt;

    /// <summary>
    /// 创建实时日志数据源
    /// </summary>
    public static LogSourceModel CreateLive() =>
        new(LogSourceKind.Live, "实时日志", null, null);

    /// <summary>
    /// 创建文件日志数据源
    /// </summary>
    public static LogSourceModel CreateFile(string path) =>
        new(LogSourceKind.File, Path.GetFileName(path), path, File.GetLastWriteTime(path));
}
