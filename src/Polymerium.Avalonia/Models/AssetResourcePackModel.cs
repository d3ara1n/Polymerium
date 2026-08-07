using System.IO;
using Avalonia.Media.Imaging;

namespace Polymerium.Avalonia.Models;

public class AssetResourcePackModel(FileInfo file, Bitmap icon, AssetResourcePackMetadataModel metadata, bool isLocked)
    : FileAssetModel<AssetResourcePackMetadataModel>(file, icon, metadata, isLocked)
{
    public string PackFormat => Metadata.PackFormat?.ToString() ?? LanguageManager.Instance.Enum_Unknown.Current();
    public string Description => Metadata.Description ?? LanguageManager.Instance.Enum_Unknown.Current();
}
