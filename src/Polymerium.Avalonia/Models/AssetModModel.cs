using System.IO;
using Avalonia.Media.Imaging;
using Resources = Polymerium.Avalonia.Properties.Resources;

namespace Polymerium.Avalonia.Models;

public class AssetModModel(FileInfo file, Bitmap icon, AssetModeMetadataModel metadata, bool isLocked)
    : FileAssetModel<AssetModeMetadataModel>(file, icon, metadata, isLocked)
{
    public override string DisplayName => Metadata.Name ?? base.DisplayName;

    public string Version => Metadata.Version ?? Resources.Enum_Unknown;
    public string Description => Metadata.Description ?? Resources.Enum_Unknown;

    public string Author =>
        Metadata.Authors is { Length: > 0 } ? string.Join(", ", Metadata.Authors) : Resources.Enum_Unknown;
}
