using System.IO;
using Avalonia.Media.Imaging;

namespace Polymerium.Avalonia.Models;

public class AssetModModel(FileInfo file, Bitmap icon, AssetModeMetadataModel metadata, bool isLocked)
    : FileAssetModel<AssetModeMetadataModel>(file, icon, metadata, isLocked)
{
    public override string DisplayName => Metadata.Name ?? base.DisplayName;

    public string Version => Metadata.Version ?? LanguageManager.Instance.Enum_Unknown.Current();
    public string Description => Metadata.Description ?? LanguageManager.Instance.Enum_Unknown.Current();

    public string Author =>
        Metadata.Authors is { Length: > 0 } ? string.Join(", ", Metadata.Authors) : LanguageManager.Instance.Enum_Unknown.Current();
}
