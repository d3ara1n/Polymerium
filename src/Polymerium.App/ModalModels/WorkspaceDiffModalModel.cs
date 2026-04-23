using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.App.Facilities;
using Polymerium.App.Models;

namespace Polymerium.App.ModalModels;

public partial class WorkspaceDiffModalModel(IViewContext<WorkspaceChangeModel> context) : ViewModelBase
{
    #region Direct

    public WorkspaceChangeModel Model { get; } = context.GetRequiredParameter();

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial string? ImportText { get; private set; }

    [ObservableProperty]
    public partial string? LiveText { get; private set; }

    #endregion

    #region Overrides

    public override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await base.InitializeAsync(cancellationToken);

        LiveText = File.Exists(Model.LivePath) ? await File.ReadAllTextAsync(Model.LivePath, cancellationToken) : null;

        ImportText = File.Exists(Model.ImportPath)
                         ? await File.ReadAllTextAsync(Model.ImportPath, cancellationToken)
                         : null;
    }

    #endregion
}
