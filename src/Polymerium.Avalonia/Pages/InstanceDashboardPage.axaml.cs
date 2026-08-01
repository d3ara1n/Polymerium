using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using ObservableCollections;
using Polymerium.Avalonia.Controls;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.PageModels;

namespace Polymerium.Avalonia.Pages;

public partial class InstanceDashboardPage : Subpage
{
    private InstanceDashboardPageModel? _model;

    // FilteredLogCollection 在切换日志源时会被重新赋值（旧视图已 Dispose），
    // 故需跟踪当前订阅的视图以便换绑时解绑，避免向已 Dispose 的视图挂事件。
    private NotifyCollectionChangedSynchronizedViewList<ScrapModel>? _subscribedView;

    public InstanceDashboardPage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        LogScroller.ScrollChanged += OnLogScrollChanged;
        // DetachedFromVisualTree 在页面离开视觉树（导航离开）时触发，参数类型由事件推断，
        // 故用 lambda 订阅而不直接引用 VisualTreeAttachmentEventArgs 的命名空间。
        DetachedFromVisualTree += (_, _) => DetachSubscriptions();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        DetachSubscriptions();

        if (DataContext is InstanceDashboardPageModel model)
        {
            _model = model;
            model.PropertyChanged += OnModelPropertyChanged;
            Subscribe(model.FilteredLogCollection);
        }
    }

    // NOTE: 页面离开视觉树时必须解绑。实时源视图来自 scrapService 的共享缓冲，
    //  其生命周期长于本页，不清理会让已 detach 的页面被缓冲间接保活并持续接收日志事件。
    private void DetachSubscriptions()
    {
        if (_model != null)
        {
            _model.PropertyChanged -= OnModelPropertyChanged;
            _model = null;
        }

        Subscribe(null);
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InstanceDashboardPageModel.FilteredLogCollection))
        {
            Subscribe(_model?.FilteredLogCollection);
        }
        else if (e.PropertyName == nameof(InstanceDashboardPageModel.IsAutoScroll) && _model?.IsAutoScroll == true)
        {
            Dispatcher.UIThread.Post(LogScroller.ScrollToEnd);
        }
    }

    private void Subscribe(NotifyCollectionChangedSynchronizedViewList<ScrapModel>? view)
    {
        if (_subscribedView == view)
        {
            return;
        }

        if (_subscribedView != null)
        {
            _subscribedView.CollectionChanged -= OnCollectionChanged;
        }

        _subscribedView = view;
        if (view != null)
        {
            view.CollectionChanged += OnCollectionChanged;
            // 文件源一次性载入后不再有 Add 事件，实时源挂载时缓冲可能已非空，
            // 故换绑时先钉一次到底部；Post 等面板重算 extent 后再滚动。
            Dispatcher.UIThread.Post(() =>
            {
                if (_model?.IsAutoScroll == true)
                {
                    LogScroller.ScrollToEnd();
                }
            });
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Reset 覆盖过滤/搜索变化导致的视图整体重建，跟随开启时同样保持钉底。
        if (e.Action is not (NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Reset))
        {
            return;
        }

        if (_model?.IsAutoScroll == true)
        {
            // 集合已变但 VirtualizingStackPanel 尚未重算 extent，同步 ScrollToEnd 会停在旧底部，故延后执行。
            Dispatcher.UIThread.Post(LogScroller.ScrollToEnd);
        }
    }

    // NOTE: 用 OffsetDelta 的符号而非仅位置判断，是关键不直观点。
    //  跟随开启时新日志使 extent 增长，ScrollChanged 会因 extent 变化再次触发，但 OffsetDelta.Y 为 0；
    //  若只看「是否在底部」会把这次事件误判为「离开底部」而错误关闭跟随，故仅在用户主动滚动（Delta 非零）时切换。
    private void OnLogScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_model == null)
        {
            return;
        }

        var atBottom = LogScroller.Viewport.Height > 0 && LogScroller.Offset.Y >= LogScroller.ScrollBarMaximum.Y - 0.5d;
        if (e.OffsetDelta.Y < 0 && !atBottom)
        {
            _model.IsAutoScroll = false;
        }
        else if (e.OffsetDelta.Y > 0 && atBottom)
        {
            _model.IsAutoScroll = true;
        }
    }
}
