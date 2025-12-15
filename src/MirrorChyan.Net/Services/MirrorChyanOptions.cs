namespace MirrorChyan.Net.Services;

public class MirrorChyanOptions
{
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
}
