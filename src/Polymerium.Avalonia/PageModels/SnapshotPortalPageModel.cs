using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.ModalModels;
using Polymerium.Avalonia.Services;

namespace Polymerium.Avalonia.PageModels;

public partial class SnapshotPortalPageModel(
    IViewContext<SnapshotsModalModel.SnapshotContext> context,
    NotificationService notificationService) : ViewModelBase
{
    #region Direct

    public SnapshotsModalModel.SnapshotContext Context { get; } = context.Parameter!;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial int SnapshotCount { get; set; }

    [ObservableProperty]
    public partial string? LatestTimeText { get; set; }

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        try
        {
            var snapshots = Context.Handle.List();
            SnapshotCount = snapshots.Count;
            LatestTimeText = snapshots.Count > 0
                ? snapshots[0].CreatedAt.Humanize(false)
                : null;
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, "Failed to load snapshot info");
        }

        return Task.CompletedTask;
    }

    #endregion
}
