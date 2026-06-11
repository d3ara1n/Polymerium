using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class InstancePackageBuildStatusResultModel : ModelBase
{
    #region Direct

    public required bool IsBuilt { get; init; }

    public required bool IsSkipped { get; init; }

    public required string Target { get; init; }

    #endregion
}
