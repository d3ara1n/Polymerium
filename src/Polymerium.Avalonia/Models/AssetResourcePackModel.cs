using System.IO;
using System;

namespace Polymerium.Avalonia.Models;

public class AssetResourcePackModel(FileInfo file, Uri icon, AssetResourcePackMetadataModel metadata, bool isLocked)
    : FileAssetModel<AssetResourcePackMetadataModel>(file, icon, metadata, isLocked)
{
    public string PackFormat => Metadata.PackFormat?.ToString() ?? LanguageManager.Instance.Enum_Unknown.Current();
    public string Description => Metadata.Description ?? LanguageManager.Instance.Enum_Unknown.Current();
}
