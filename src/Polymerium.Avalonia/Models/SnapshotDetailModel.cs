using System;
using Humanizer;

namespace Polymerium.Avalonia.Models;

public class SnapshotDetailModel
{
    public required string Label { get; init; }
    public required string Remark { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string GameVersion { get; init; }
    public required string Loader { get; init; }
    public required int PackageCount { get; init; }
    public required int FileCount { get; init; }
    public required long TotalSize { get; init; }
    public required SnapshotDiffModel? Diff { get; init; }

    public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    public string TotalSizeFormatted => ByteSize.FromBytes(TotalSize).Humanize();
}
