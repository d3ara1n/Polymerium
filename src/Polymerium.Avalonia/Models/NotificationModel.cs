using System;
using System.Threading;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

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

    [ObservableProperty]
    public partial Uri? Thumbnail { get; set; }

    public AvaloniaList<GrowlAction> Actions { get; init; } = [];

    #endregion

    #region Direct

    public Guid Id { get; } = Guid.NewGuid();
    public required string Title { get; init; }
    public required GrowlLevel Level { get; init; }
    public required DateTimeOffset PublishedAtRaw { get; init; }
    public string PublishedAt => PublishedAtRaw.ToLocalTime().ToString("HH:mm:ss");

    public CancellationToken Token => _cts.Token;

    #endregion

    #region Lifecycles

    private readonly CancellationTokenSource _cts = new();

    internal void OnRemoved()
    {
        // NOTE: 生命周期由外部（MainWindowContext）维护，被移除即用户失去控制权，
        //  所以这里要连带取消与该通知相关的任务。
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }

        _cts.Dispose();
    }

    public void Cancel()
    {
        // NOTE: 除 Remove 外用户也可提前 Cancel（如按钮），此后 Action 全部不可用，UI 显示“过期”。
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            Dispatcher.UIThread.Post(() => IsCancelled = true);
        }
    }

    #endregion
}
