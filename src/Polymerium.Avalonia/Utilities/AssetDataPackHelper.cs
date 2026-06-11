using Avalonia.Media.Imaging;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Utilities;

/// <summary>
///     数据包元数据解析器
/// </summary>
public static class AssetDataPackHelper
{
    /// <summary>
    ///     从 zip 文件中解析数据包元数据
    /// </summary>
    public static AssetDataPackMetadataModel ParseMetadata(string zipFilePath) =>
        AssetArchiveHelper.ParsePackMetadata<AssetDataPackMetadataModel>(zipFilePath);

    /// <summary>
    ///     从 zip 文件中提取数据包图标（pack.png）
    /// </summary>
    public static Bitmap? ExtractIcon(string zipFilePath) =>
        AssetArchiveHelper.ExtractIcon(zipFilePath, "pack.png");
}
