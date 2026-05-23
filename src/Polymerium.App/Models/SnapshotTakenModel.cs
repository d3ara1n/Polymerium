using System.Collections.Generic;
using Polymerium.App.Facilities;
using TridentCore.Abstractions.Snapshots;
using TridentCore.Abstractions.Utilities;

namespace Polymerium.App.Models;

public class SnapshotTakenModel : ModelBase
{
    #region Direct

    public required (SnapshotInfo Snapshot, IReadOnlyList<ReferenceInfo> References) Metadata { get; init; }

    public required IReadOnlyList<FilePartitionModel> Partitions { get; init; }

    public int PackageCount => Metadata.Snapshot.PackageCount;

    public int FileCount => Metadata.Snapshot.FileCount;

    public long TotalSize => Metadata.Snapshot.TotalSize;

    public string LoaderLabel => LoaderHelper.ToDisplayLabel(Metadata.Snapshot.Metadata.Loader);

    public string VersionLabel => Metadata.Snapshot.Metadata.Version;

    #endregion
}
