using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Huskui.Avalonia.Behaviors;

public class ScrollViewerEdgeFadeBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<double> FadedEdgeThicknessProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewerEdgeFadeBehavior, ScrollViewer, double>("FadedEdgeThickness",
            20d,
            false,
            BindingMode.OneTime);

    static ScrollViewerEdgeFadeBehavior()
    {
        FadedEdgeThicknessProperty.Changed.AddClassHandler<ScrollViewer>(OnFadeThicknessChanged);
    }

    private static void OnFadeThicknessChanged(ScrollViewer sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is double thickness)
            sender.ScrollChanged += SenderOnScrollChanged;
    }

    private static void SenderOnScrollChanged(object? o, ScrollChangedEventArgs e)
    {
        if (o is ScrollViewer scrollViewer)
        {
            var thickness = scrollViewer.GetValue(FadedEdgeThicknessProperty);
            var x = scrollViewer.Offset.X;
            var y = scrollViewer.Offset.Y;
            var startX = x < thickness ? x : thickness;
            var startY = y < thickness ? y : thickness;
        }
    }
}