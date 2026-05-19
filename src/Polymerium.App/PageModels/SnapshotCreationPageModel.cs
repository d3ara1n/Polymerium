using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.App.Facilities;
using Polymerium.App.ModalModels;
using Polymerium.App.Models;
using TridentCore.Core.Services;

namespace Polymerium.App.PageModels;

public partial class SnapshotCreationPageModel(IViewContext<SnapshotsModalModel.SnapshotContext> context) : ViewModelBase
{
    #region Direct

    public SnapshotManager.InstanceSnapshots Handle { get; } = context.Parameter!.Handle;
    public InstanceBasicModel Basic { get; } = context.Parameter!.Basic;
    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsSnapshotTaking { get; set; }

    [ObservableProperty]
    public partial SnapshotTakenModel? SnapshotTaken { get; set; }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task TakeAsync()
    {
        // TODO: IProcess report to ui
        var collected = new Progress<int>();
        var processed = new Progress<int>();
        await Handle.TakeAsync(collected, processed);

        // TODO: set snapshot model
        // _ = new SnapshotModel()
    }
    #endregion
}
