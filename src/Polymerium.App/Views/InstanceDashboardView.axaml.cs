using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Polymerium.App.Controls;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public partial class InstanceDashboardView : Subpage
{
    private int _debounce;
    private ScrollViewer? _logViewer;
    private DispatcherTimer? _scrollTimer;

    public InstanceDashboardView()
    {
        InitializeComponent();
        AddHandler(ScrollViewer.ScrollChangedEvent, OnScrollChanged);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _logViewer = this.FindControl<ScrollViewer>("LogViewer");

        // 使用定时器节流滚动，每100ms最多滚动一次
        _scrollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _scrollTimer.Tick += OnScrollTimerTick;
        _scrollTimer.Start();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _scrollTimer?.Stop();
        _scrollTimer = null;

        RemoveHandler(ScrollViewer.ScrollChangedEvent, OnScrollChanged);
        base.OnUnloaded(e);
    }

    private void OnScrollTimerTick(object? sender, EventArgs e)
    {
        if (DataContext is InstanceDashboardViewModel vm && vm.IsAutoScroll && _logViewer != null)
        {
            // 检查是否已经在底部，避免不必要的滚动
            var extent = _logViewer.Extent.Height;
            var viewport = _logViewer.Viewport.Height;
            var offset = _logViewer.Offset.Y;

            // 如果不在底部附近（允许5像素误差），则滚动到底部
            if (extent > viewport && extent - offset - viewport > 5)
            {
                _logViewer.ScrollToEnd();
            }
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        e.Handled = true;

        // 检测用户向上滚动，自动关闭自动滚动
        if (e.OffsetDelta.Y < 0)
        {
            _debounce++;
            if (_debounce > 3 && DataContext is InstanceDashboardViewModel vm)
            {
                vm.IsAutoScroll = false;
                _debounce = 0;
            }
        }
        else
        {
            _debounce = 0;
        }
    }
}
