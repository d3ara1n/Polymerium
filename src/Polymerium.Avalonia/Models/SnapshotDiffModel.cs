namespace Polymerium.Avalonia.Models;

public class SnapshotDiffModel
{
    public required int FilesAdded { get; init; }
    public required int FilesRemoved { get; init; }
    public required int PackagesAdded { get; init; }
    public required int PackagesRemoved { get; init; }
}
