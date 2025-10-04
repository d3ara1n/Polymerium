using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.App.Facilities;
using Resources = Polymerium.App.Properties.Resources;

namespace Polymerium.App.Models;

public partial class AssetResourcePackModel : ModelBase
{
    public AssetResourcePackModel(FileInfo file, Bitmap icon, AssetResourcePackMetadataModel metadata, bool isLocked)
    {
        FilePath = file.FullName;
        FileSizeRaw = file.Length;
        LastModifiedRaw = file.LastWriteTime;
        Icon = icon;
        Metadata = metadata;
        IsLocked = isLocked;
    }

    #region Direct

    public string FileName => Path.GetFileName(FilePath);
    public Bitmap Icon { get; }
    public long FileSizeRaw { get; }
    public DateTimeOffset LastModifiedRaw { get; }
    public string LastModified => LastModifiedRaw.Humanize();
    public AssetResourcePackMetadataModel Metadata { get; }
    public bool IsLocked { get; }

    public string FileSize => ByteSize.FromBytes(FileSizeRaw).ToString("0.#");

    public string LastModifiedFormatted => LastModifiedRaw.ToString("g");

    public string DisplayName => Path.GetFileNameWithoutExtension(FileName);

    public string PackFormat => Metadata.PackFormat?.ToString() ?? Resources.Enum_Unknown;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName))]
    public partial string FilePath { get; set; }

    #endregion
}
