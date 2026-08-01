using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Polymerium.Avalonia.Controls;

namespace Polymerium.Avalonia.Pages;

public partial class InstanceDashboardPage : Subpage
{
    public static readonly DirectProperty<InstanceDashboardPage, bool> IsAutoScrollProperty =
        AvaloniaProperty.RegisterDirect<InstanceDashboardPage, bool>(
            nameof(IsAutoScroll),
            o => o.IsAutoScroll,
            (o, v) => o.IsAutoScroll = v,
            true);

    public bool IsAutoScroll
    {
        get;
        set => SetAndRaise(IsAutoScrollProperty, ref field, value);
    } = true;

    private bool _scrollPending;   // 钉底请求合并标志，配合 RequestScrollToEnd 做节流

    private int _disableDebounce;  // 关闭跟随的滞回计数

    public InstanceDashboardPage()
    {
        InitializeComponent();
        ((INotifyCollectionChanged)LogList.Items).CollectionChanged += OnItemsChanged;
        LogScroller.ScrollChanged += OnLogScrollChanged;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsAutoScrollProperty && change.NewValue is true)
        {
            RequestScrollToEnd();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ((INotifyCollectionChanged)LogList.Items).CollectionChanged -= OnItemsChanged;
        LogScroller.ScrollChanged -= OnLogScrollChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Reset 覆盖过滤/搜索变化导致的视图整体重建，跟随开启时同样保持钉底。
        if (e.Action is not (NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Reset))
        {
            return;
        }

        if (IsAutoScroll)
        {
            RequestScrollToEnd();
        }
    }

    // 合并同一调度周期内的多次钉底请求：burst 新增只触发一次 ScrollToEnd，避免对每条日志都重排布局。
    private void RequestScrollToEnd()
    {
        if (_scrollPending)
        {
            return;
        }

        _scrollPending = true;
        // 集合已变但 VirtualizingStackPanel 尚未重算 extent，同步 ScrollToEnd 会停在旧底部，故延后执行。
        Dispatcher.UIThread.Post(() =>
        {
            _scrollPending = false;
            LogScroller.ScrollToEnd();
        });
    }

    // NOTE: 用 OffsetDelta 的符号而非仅位置判断，是关键不直观点。
    //  跟随开启时新日志使 extent 增长，ScrollChanged 会因 extent 变化再次触发，但 OffsetDelta.Y 为 0；
    //  若只看「是否在底部」会把这次事件误判为「离开底部」而错误关闭跟随，故仅在用户主动滚动（Delta 非零）时切换。
    private void OnLogScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        var atBottom = LogScroller.Viewport.Height > 0 && LogScroller.Offset.Y >= LogScroller.ScrollBarMaximum.Y - 0.5d;
        if (e.OffsetDelta.Y < 0 && !atBottom)
        {
            _disableDebounce++;
            // 连续多次向上滚才判定为有意回看，避免惯性/触控板抖动误关跟随
            if (_disableDebounce > 1)
            {
                IsAutoScroll = false;
                _disableDebounce = 0;
            }
        }
        else
        {
            _disableDebounce = 0;
            if (e.OffsetDelta.Y > 0 && atBottom)
            {
                IsAutoScroll = true;
            }
        }
    }
}
