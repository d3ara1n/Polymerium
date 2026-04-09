using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia.Models;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class NotificationModel : ModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial bool IsRead { get; set; }

    [ObservableProperty]
    public required partial string Message { get; set; }

    [ObservableProperty]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial bool IsProgressBarVisible { get; set; }

    [ObservableProperty]
    public partial bool IsProgressIndeterminate { get; set; }

    [ObservableProperty]
    public partial bool IsCancelled { get; set; }
    public AvaloniaList<GrowlAction> Actions { get; init; } = [];

    #endregion

    #region Direct

    public Guid Id { get; } = Guid.NewGuid();
    public required string Title { get; init; }
    public required GrowlLevel Level { get; init; }
    public required DateTimeOffset PublishedAtRaw { get; init; }
    public required IBrush AccentBrush { get; init; }
    public required Bitmap? Thumbnail { get; init; }
    public string PublishedAt => PublishedAtRaw.ToLocalTime().ToString("MM-dd HH:mm");

    public CancellationToken Token => _cts.Token;

    #endregion

    #region Lifecycles

    private readonly CancellationTokenSource _cts = new();

    internal void OnRemoved()
    {
        // Model 的生命周期是外部维护的(MainWindowContext)，和 UI 绑定，被移除时表示用户失去控制权，也就理所当然需要通知和这条通知有关的任务统统取消掉
        // 自身不维护生命周期
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
    }

    public void Cancel()
    {
        // 除了 Remove 时取消，也可以由用户（例如按钮事件）提前调用 Cancel，这会导致 Action 全部不可操作，UI上会显示一个“过期”
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            Dispatcher.UIThread.Post(() => IsCancelled = true);
        }
    }

    #endregion
}
