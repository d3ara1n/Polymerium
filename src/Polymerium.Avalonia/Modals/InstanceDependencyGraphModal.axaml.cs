using System;
using Huskui.Avalonia.Controls;

namespace Polymerium.Avalonia.Modals;

public partial class InstanceDependencyGraphModal : Modal
{
    private bool _fitted;

    public InstanceDependencyGraphModal()
    {
        InitializeComponent();
        // GraphPanel 的 MSAGL 布局在 MeasureOverride 里同步完成，Arrange 后 Bounds 即有效。
        // 监听 LayoutUpdated，第一次拿到有效尺寸时做一次自适应，然后取消订阅。
        GraphPanel.LayoutUpdated += OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_fitted)
        {
            return;
        }

        // 需 ZoomView 与 GraphPanel 都有有效尺寸；Graph 非空才会触发实际布局。
        if (GraphPanel.Bounds.Width <= 0 || ZoomView.Bounds.Width <= 0)
        {
            return;
        }

        ZoomView.FitToContent();
        _fitted = true;
        GraphPanel.LayoutUpdated -= OnLayoutUpdated;
    }
}
