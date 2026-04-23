using System.IO;
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
    public partial string? OldText { get; private set; }

    [ObservableProperty]
    public partial string? NewText { get; private set; }

    #endregion

    #region Overrides

    public override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await base.InitializeAsync(cancellationToken);

        OldText = File.Exists(Model.ImportPath)
            ? await File.ReadAllTextAsync(Model.ImportPath, cancellationToken).ConfigureAwait(false)
            : null;

        NewText = File.Exists(Model.LivePath)
            ? await File.ReadAllTextAsync(Model.LivePath, cancellationToken).ConfigureAwait(false)
            : null;
    }

    #endregion
}
