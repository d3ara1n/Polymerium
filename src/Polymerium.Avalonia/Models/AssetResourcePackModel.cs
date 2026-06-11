using System.IO;
using Avalonia.Media.Imaging;
using Resources = Polymerium.Avalonia.Properties.Resources;

namespace Polymerium.Avalonia.Models;

public class AssetResourcePackModel(
    FileInfo file,
    Bitmap icon,
    AssetResourcePackMetadataModel metadata,
    bool isLocked
) : FileAssetModel<AssetResourcePackMetadataModel>(file, icon, metadata, isLocked)
{
    public string PackFormat => Metadata.PackFormat?.ToString() ?? Resources.Enum_Unknown;
    public string Description => Metadata.Description ?? Resources.Enum_Unknown;
}
