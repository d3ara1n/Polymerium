using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.App.Facilities;
using Polymerium.App.ModalModels;
using Polymerium.App.Models;
using Polymerium.App.Services;
using TridentCore.Core.Services;

namespace Polymerium.App.PageModels;

public partial class SnapshotCreationPageModel(
    IViewContext<SnapshotsModalModel.SnapshotContext> context,
    NotificationService notificationService) : ViewModelBase
{
    #region Direct

    public SnapshotsModalModel.SnapshotContext Context { get; } = context.Parameter!;

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

    [ObservableProperty]
    public partial string Label { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Remark { get; set; } = string.Empty;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task TakeAsync()
    {
        var collected = new Progress<int>(x => TotalCollected = x);
        var processed = new Progress<int>(x => TotalProcessed = x);
        var metadata = await Context.Handle.TakeAsync(collected, processed);

        SnapshotTaken = new() { Metadata = metadata };
    }

    private bool CanCreate(SnapshotTakenModel? model) => model != null;

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateAsync(SnapshotTakenModel? model)
    {
        if (model == null)
        {
            return;
        }

        var (snapshot, references) = model.Metadata;
        snapshot = snapshot with { Label = !string.IsNullOrEmpty(Label) ? Label : "Untitled", Remark = Remark };

        try
        {
            await Context.Handle.CommitAsync(snapshot, references);
            notificationService.PopMessage($"{snapshot.Label} has been saved.", "Snapshot created", GrowlLevel.Success);
            Context.BackHandler.Invoke();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, "Create snapshot failed");
        }
    }

    #endregion
}
