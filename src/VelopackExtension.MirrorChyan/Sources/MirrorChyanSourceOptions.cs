using MirrorChyan.Net.Models;

namespace VelopackExtension.MirrorChyan.Sources;

/// <summary>
/// MirrorChyan 更新源的配置选项
/// 使用 IOptionsMonitor 允许在运行时修改
/// </summary>
public class MirrorChyanSourceOptions
{
    /// <summary>
    /// MirrorChyan CDK 密钥，用于加速下载
    /// 可为 null，表示使用免费通道
    /// </summary>
    public string? Cdk { get; set; }

    /// <summary>
    /// MirrorChyan 更新通道
    /// </summary>
    public ChannelKind Channel { get; set; } = ChannelKind.Stable;
}
