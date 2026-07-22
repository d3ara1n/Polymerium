using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public abstract partial class FileAssetModel(FileInfo file, Bitmap icon, bool isLocked) : ModelBase
{
    public string FileName => Path.GetFileName(FilePath);
    public Bitmap Icon { get; } = icon;
    public long FileSizeRaw { get; } = file.Length;
    public DateTimeOffset LastModifiedRaw { get; } = file.LastWriteTime;
    public string LastModified => LastModifiedRaw.Humanize();
    public bool IsLocked { get; } = isLocked;
    public string FileSize => ByteSize.FromBytes(FileSizeRaw).ToString("0.#");
    public string LastModifiedFormatted => LastModifiedRaw.ToString("g");
    public virtual string DisplayName => Path.GetFileNameWithoutExtension(FileName);

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName))]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial string FilePath { get; set; } = file.FullName;
}

public abstract class FileAssetModel<TMetadata>(FileInfo file, Bitmap icon, TMetadata metadata, bool isLocked)
    : FileAssetModel(file, icon, isLocked)
{
    public TMetadata Metadata { get; } = metadata;
}
