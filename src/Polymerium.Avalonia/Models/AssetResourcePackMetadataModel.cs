namespace Polymerium.Avalonia.Models;

/// <summary>
///     资源包元数据模型（从 pack.mcmeta 中读取）
/// </summary>
public class AssetResourcePackMetadataModel : IAssetPackMetadataModel
{
    public string? Description { get; set; }
    public int? PackFormat { get; set; }
}
