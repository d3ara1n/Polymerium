using System.IO;
using Avalonia.Media.Imaging;

namespace Polymerium.Avalonia.Models;

public class AssetDataPackModel(FileInfo file, Bitmap icon, AssetDataPackMetadataModel metadata, bool isLocked)
    : FileAssetModel<AssetDataPackMetadataModel>(file, icon, metadata, isLocked)
{
    public string PackFormat => Metadata.PackFormat?.ToString() ?? LanguageManager.Instance.Enum_Unknown.Current();
    public string Description => Metadata.Description ?? LanguageManager.Instance.Enum_Unknown.Current();
}
