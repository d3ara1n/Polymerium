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

    // 当前正在订阅 CollectionChanged 的视图；FilteredLogCollection 会被重新赋值，
    // 因此需要跟踪旧视图以便解绑，避免事件泄漏到已 Dispose 的视图上。
    private NotifyCollectionChangedSynchronizedViewList<ScrapModel>? _subscribedView;

    public InstanceDashboardPage()
    {
        InitializeComponent();
        // DataContext 由 activator 在进入视觉树时设置，必须等它到达后才能挂到模型上。
        DataContextChanged += OnDataContextChanged;
        // 直接订阅日志 ScrollViewer 本身（而不是在页面级 AddHandler），
        // 因为底部检测需要读取它的 Offset / ScrollBarMaximum。
        LogScroller.ScrollChanged += OnLogScrollChanged;
    }

    // 每次导航都会由 activator 创建全新的页面与模型；DataContext 变化时解绑旧模型、挂上新模型。
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_model != null)
        {
            _model.PropertyChanged -= OnModelPropertyChanged;
            _model = null;
        }

        Subscribe(null);

        if (DataContext is InstanceDashboardPageModel model)
        {
            _model = model;
            model.PropertyChanged += OnModelPropertyChanged;
            Subscribe(model.FilteredLogCollection);
        }
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // UpdateLogSource 会在切换日志源/启动/结束时重新赋值 FilteredLogCollection（旧视图已 Dispose），
        // 这里必须跟着换绑 CollectionChanged，否则新视图的新增日志不会触发跟随滚动。
        if (e.PropertyName == nameof(InstanceDashboardPageModel.FilteredLogCollection))
        {
            Subscribe(_model?.FilteredLogCollection);
        }
        // 用户点击切换按钮重新开启跟随时，无论当前在何处都应立刻跳到底部。
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
            // 视图刚就位时钉一次到底部：文件日志源是一次性整体载入的，可能不会再有 Add 事件，
            // 而实时源在 OnInitializeAsync 挂载时缓冲可能已非空；Post 延迟到布局完成后执行。
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
        // Reset 也触发跟随：过滤开关/搜索变化时视图会整体重建，跟随开启时应保持钉底；
        // 环形缓冲满时淘汰头部也会以 Reset/Add 形式体现，与 toast 的既有行为保持一致。
        if (e.Action is not (NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Reset))
        {
            return;
        }

        if (_model?.IsAutoScroll == true)
        {
            // Post 而非同步调用：集合已变化但 VirtualizingStackPanel 的 extent 尚未重新布局，
            // 立即 ScrollToEnd 会停在旧的底部；延迟到消息队列尾部执行才能拿到最新 extent。
            Dispatcher.UIThread.Post(LogScroller.ScrollToEnd);
        }
    }

    // NOTE: 用 OffsetDelta 的符号而非仅位置判断，是关键不直观点——
    // 跟随开启时新日志使 extent 增长，ScrollChanged 会因 extent 变化触发，但 OffsetDelta.Y 为 0；
    // 若仅看"是否在底部"，该事件会误判为"离开底部"而关闭跟随。因此只在用户主动滚动（Delta 非零）时切换状态。
    private void OnLogScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_model == null)
        {
            return;
        }

        var atBottom = LogScroller.Viewport.Height > 0 && LogScroller.Offset.Y >= LogScroller.ScrollBarMaximum.Y - 0.5d;
        if (e.OffsetDelta.Y < 0 && !atBottom)
        {
            // 用户向上滚动离开底部：说明在回看历史日志，停止跟随（按钮随之取消勾选）。
            _model.IsAutoScroll = false;
        }
        else if (e.OffsetDelta.Y > 0 && atBottom)
        {
            // 用户主动滚回到底部：恢复跟随，对应"滚动条在最下方时持续滚动到最新日志"。
            _model.IsAutoScroll = true;
        }
    }
}
