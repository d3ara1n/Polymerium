using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.App.Facilities;
using Resources = Polymerium.App.Properties.Resources;

namespace Polymerium.App.Models;

public partial class AssetModModel : ModelBase
{
    public AssetModModel(FileInfo file, Bitmap icon, AssetModeMetadataModel metadata)
    {
        FilePath = file.FullName;
        FileSizeRaw = file.Length;
        LastModifiedRaw = file.LastWriteTime;
        Icon = icon;
        Metadata = metadata;
    }

    #region Direct

    public string FileName => Path.GetFileName(FilePath);
    public Bitmap Icon { get; }
    public long FileSizeRaw { get; }
    public DateTimeOffset LastModifiedRaw { get; }
    public string LastModified => LastModifiedRaw.Humanize();
    public AssetModeMetadataModel Metadata { get; }

    public string FileSize => ByteSize.FromBytes(FileSizeRaw).ToString("0.#");

    public string LastModifiedFormatted => LastModifiedRaw.ToString("g");

    public string DisplayName => Metadata.Name ?? Path.GetFileNameWithoutExtension(FileName);

    public string Version => Metadata.Version ?? Resources.Enum_Unknown;

    public string Author =>
        Metadata.Authors is { Length: > 0 } ? string.Join(", ", Metadata.Authors) : Resources.Enum_Unknown;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName))]
    public partial string FilePath { get; set; }

    #endregion
}
