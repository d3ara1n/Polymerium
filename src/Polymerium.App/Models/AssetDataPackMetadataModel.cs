namespace Polymerium.App.Models;

/// <summary>
///     数据包元数据模型（从 pack.mcmeta 中读取）
/// </summary>
public class AssetDataPackMetadataModel
{
    public string? Description { get; set; }
    public int? PackFormat { get; set; }
}
