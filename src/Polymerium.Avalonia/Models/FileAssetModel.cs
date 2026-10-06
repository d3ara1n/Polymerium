using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public abstract partial class FileAssetModel(FileInfo file, Uri icon, bool isLocked) : ModelBase
{
    public string FileName => Path.GetFileName(FilePath);
    public long FileSizeRaw { get; } = file.Length;
    public DateTimeOffset LastModifiedRaw { get; } = file.LastWriteTime;
    public string LastModified => LastModifiedRaw.Humanize();
    public bool IsLocked { get; } = isLocked;
    public string FileSize => ByteSize.FromBytes(FileSizeRaw).ToString("0.#");
    public string LastModifiedFormatted => LastModifiedRaw.ToString("g");
    public virtual string DisplayName => Path.GetFileNameWithoutExtension(FileName);

    [ObservableProperty]
    public partial Uri Icon { get; set; } = icon;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName))]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial string FilePath { get; set; } = file.FullName;
}

public abstract class FileAssetModel<TMetadata>(FileInfo file, Uri icon, TMetadata metadata, bool isLocked)
    : FileAssetModel(file, icon, isLocked)
{
    public TMetadata Metadata { get; } = metadata;
}
