using Avalonia.Media.Imaging;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Utilities;

/// <summary>
///     资源包元数据解析器
/// </summary>
public static class AssetResourcePackHelper
{
    /// <summary>
    ///     从 zip 文件中解析资源包元数据
    /// </summary>
    public static AssetResourcePackMetadataModel ParseMetadata(string zipFilePath) =>
        AssetArchiveHelper.ParsePackMetadata<AssetResourcePackMetadataModel>(zipFilePath);

    /// <summary>
    ///     从 zip 文件中提取资源包图标（pack.png）
    /// </summary>
    public static Bitmap? ExtractIcon(string zipFilePath) =>
        AssetArchiveHelper.ExtractIcon(zipFilePath, "pack.png");
}
