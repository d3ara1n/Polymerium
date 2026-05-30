using System;
using TridentCore.Abstractions.Snapshots;

namespace Polymerium.App.Models;

public class SnapshotItemModel
{
    public required SnapshotInfo Source { get; init; }
    public required SnapshotItemModel? Previous { get; init; }

    public string DisplayLabel => string.IsNullOrEmpty(Source.Label) ? "Untitled" : Source.Label;
    public string CreatedAtText => Source.CreatedAt.ToString("yyyy-MM-dd HH:mm");

    public SnapshotItemModel WithPrevious(SnapshotItemModel? previous) =>
        new() { Source = Source, Previous = previous };
}
