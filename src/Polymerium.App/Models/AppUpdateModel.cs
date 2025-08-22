using Polymerium.App.Facilities;
using Velopack;

namespace Polymerium.App.Models;

public class AppUpdateModel(UpdateInfo update) : ModelBase
{
    #region Direct

    public UpdateInfo Update => update;

    public bool IsPrerelease => update.TargetFullRelease.Version.IsPrerelease;

    public string Version =>
        $"{update.TargetFullRelease.Version.Major}.{update.TargetFullRelease.Version.Minor}.{update.TargetFullRelease.Version.Patch}{(update.TargetFullRelease.Version.HasMetadata ? $"-{update.TargetFullRelease.Version.Metadata}" : string.Empty)}";

    #endregion
}