using MirrorChyan.Net.Models;

namespace MirrorChyan.Net.Services;

public class MirrorChyanOptions
{
    /// <summary>
    /// 服务器终结点
    /// </summary>
    public Uri BaseAddress { get; set; } = new("https://mirrorchyan.com");

    /// <summary>
    /// 产品资源名，例如 MAA
    /// </summary>
    public required string ProductId { get; set; }

    /// <summary>
    /// 客户端名称，用于营销统计，例如 MaaWpfGui
    /// </summary>
    public required string ClientName { get; set; }

    /// <summary>
    /// 当前版本字符串，例如 v1.0.0
    /// </summary>
    public required string VersionString { get; set; }

    /// <summary>
    /// 系统字符串，例如 win
    /// </summary>
    public required string Os { get; set; } = Oses.FromPlatform();

    /// <summary>
    /// 架构字符串，例如 x86
    /// </summary>
    public required string Arch { get; set; } = Arches.FromPlatform();

    /// <summary>
    /// 是否启用增量包，增量包的更新逻辑需要自行适配
    /// </summary>
    public required bool IsIncrementalEnabled { get; set; } = true;
}
