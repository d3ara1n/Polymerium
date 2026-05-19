using System;
using System.Collections.Generic;
using Humanizer;
using Polymerium.App.Facilities;
using TridentCore.Abstractions.Snapshots;

namespace Polymerium.App.Models;

public class SnapshotTakenModel: ModelBase
{
    public required (SnapshotInfo Snapshot, IReadOnlyList<ReferenceInfo> References) Metadata { get; init; }

    public DateTime TakenAtRaw => Metadata.Snapshot.CreatedAt;

    public string TakenAt => TakenAtRaw.Humanize();
}
