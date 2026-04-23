using System.Threading;
using System.Threading.Tasks;
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

    #region Overrides

    public override Task InitializeAsync(CancellationToken cancellationToken) =>
        base.InitializeAsync(cancellationToken);

    #endregion
}
