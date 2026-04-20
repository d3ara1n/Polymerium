using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class WorkspaceChangeModel : ModelBase
{
    #region Direct

    public required string RelativePath { get; set; }
    public required string Name { get; set; }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial WorkspaceChangeKind Kind { get; set; }

    #endregion
}
