using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class ReleaseFileModel : ModelBase
{
    #region Direct

    public required string Name { get; init; }
    public required bool Exists { get; init; }

    #endregion
}
