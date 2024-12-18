using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":dragover")]
public class DragDropZone : ContentControl
{
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        Shape.StrokeThicknessProperty.AddOwner<DragDropZone>();

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public static readonly StyledProperty<IBrush?> StrokeBrushProperty = Shape.StrokeProperty.AddOwner<DragDropZone>();

    public IBrush? StrokeBrush
    {
        get => GetValue(StrokeBrushProperty);
        set => SetValue(StrokeBrushProperty, value);
    }

    public static readonly StyledProperty<AvaloniaList<double>?> StrokeDashArrayProperty =
        Shape.StrokeDashArrayProperty.AddOwner<DragDropZone>();

    public AvaloniaList<double>? StrokeDashArray
    {
        get => GetValue(StrokeDashArrayProperty);
        set => SetValue(StrokeDashArrayProperty, value);
    }

    public static readonly StyledProperty<double> RadiusProperty =
        AvaloniaProperty.Register<DragDropZone, double>(nameof(Radius));

    public double Radius
    {
        get => GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }
}