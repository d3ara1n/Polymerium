using System;
using Humanizer;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class InstancePackageModificationModel : ModelBase
{
    #region Direct

    public required string? VersionName { get; init; }

    public required InstancePackageModificationKind Kind { get; init; }

    public required DateTimeOffset ModifiedAtRaw { get; init; }

    public string ModifiedAt => ModifiedAtRaw.Humanize();

    #endregion
}
