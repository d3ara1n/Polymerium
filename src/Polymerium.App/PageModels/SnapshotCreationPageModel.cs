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

    [ObservableProperty]
    public partial int TotalCollected { get; set; }

    [ObservableProperty]
    public partial int TotalProcessed { get; set; }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task TakeAsync()
    {
        var collected = new Progress<int>(x => TotalCollected = x);
        var processed = new Progress<int>(x => TotalProcessed = x);
       var metadata =  await Handle.TakeAsync(collected, processed);

       SnapshotTaken = new() { Metadata = metadata };
    }

    private bool CanCreate(SnapshotTakenModel? model) => model != null;

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateAsync(SnapshotTakenModel? model)
    {
    }
    #endregion
}
