using System;
using System.Threading;
using System.Threading.Tasks;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Pages;
using Polymerium.App.Services;
using TridentCore.Core.Services;

namespace Polymerium.App.ModalModels;

public class SnapshotsModalModel : ViewModelBase
{
    public SnapshotsModalModel(IViewContext<InstanceBasicModel> context, SnapshotManager snapshotManager, NotificationService notificationService)
    {
        _snapshotManager = snapshotManager;
        _notificationService = notificationService;
        Basic = context.Parameter!;
    }

    #region Overrides

    protected override Task OnDeinitializeAsync()
    {
        _snapshots?.Dispose();
        return Task.CompletedTask;
    }

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        try
        {
            _snapshots = _snapshotManager.Open(Basic.Key);

            Context = new()
            {
                Basic = Basic,
                Handle = _snapshots,
                BackHandler = BackHandler!,
            };
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex, "Failed to open snapshots database");
        }

        NavigateHandler!.Invoke(typeof(SnapshotPortalPage));
        return Task.CompletedTask;
    }

    #endregion

    #region Fields

    private SnapshotManager.InstanceSnapshots? _snapshots;

    #endregion

    #region Injected

    private readonly SnapshotManager _snapshotManager;
    private readonly NotificationService _notificationService;

    #endregion

    #region Direct

    public InstanceBasicModel Basic { get; }
    public SnapshotContext? Context { get; private set; }
    public Action<Type>? NavigateHandler { get; internal set; }
    public Action? BackHandler { get; internal set; }
    public Action? DismissHandler { get; internal set; }

    #endregion

    #region Nested type: SnapshotContext

    public class SnapshotContext
    {
        public required InstanceBasicModel Basic { get; init; }
        public required SnapshotManager.InstanceSnapshots Handle { get; init; }
        public required Action BackHandler { get; init; }
    }

    #endregion
}
