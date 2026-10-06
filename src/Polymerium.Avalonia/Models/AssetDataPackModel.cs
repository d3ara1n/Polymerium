using System.IO;
using System;

namespace Polymerium.Avalonia.Models;

public class AssetDataPackModel(FileInfo file, Uri icon, AssetDataPackMetadataModel metadata, bool isLocked)
    : FileAssetModel<AssetDataPackMetadataModel>(file, icon, metadata, isLocked)
{
    public string PackFormat => Metadata.PackFormat?.ToString() ?? LanguageManager.Instance.Enum_Unknown.Current();
    public string Description => Metadata.Description ?? LanguageManager.Instance.Enum_Unknown.Current();
}
