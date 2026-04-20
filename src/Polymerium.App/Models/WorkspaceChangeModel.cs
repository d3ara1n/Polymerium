using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class WorkspaceChangeModel : ModelBase
{
    #region Direct

    public required string RelativePath { get; set; }
    public required string Name { get; set; }

    #endregion
}
