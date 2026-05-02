using System.IO;
using Avalonia.Media.Imaging;
using Resources = Polymerium.App.Properties.Resources;

namespace Polymerium.App.Models;

public class AssetDataPackModel(
    FileInfo file,
    Bitmap icon,
    AssetDataPackMetadataModel metadata,
    bool isLocked
) : FileAssetModel<AssetDataPackMetadataModel>(file, icon, metadata, isLocked)
{
    public string PackFormat => Metadata.PackFormat?.ToString() ?? Resources.Enum_Unknown;
    public string Description => Metadata.Description ?? Resources.Enum_Unknown;
}
