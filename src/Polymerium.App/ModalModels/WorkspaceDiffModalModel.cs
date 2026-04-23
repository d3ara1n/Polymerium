using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.App.Facilities;
using Polymerium.App.Models;

namespace Polymerium.App.ModalModels;

public class WorkspaceDiffModalModel(IViewContext<WorkspaceChangeModel> context) : ViewModelBase
{
    #region Direct

    public WorkspaceChangeModel Model { get; } = context.GetRequiredParameter();

    #endregion

    #region Reactive

    #endregion
}
