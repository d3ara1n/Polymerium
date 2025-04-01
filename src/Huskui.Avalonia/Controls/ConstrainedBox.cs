using Avalonia;
using Avalonia.Controls;

namespace Huskui.Avalonia.Controls;

public class ConstrainedBox : ContentControl
{
    public static readonly StyledProperty<double> AspectRatioProperty =
        AvaloniaProperty.Register<ConstrainedBox, double>(nameof(AspectRatio), 1.0);

    public double AspectRatio
    {
        get => GetValue(AspectRatioProperty);
        set => SetValue(AspectRatioProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = availableSize.Width;
        var height = width / AspectRatio;

        if (height > availableSize.Height)
        {
            height = availableSize.Height;
            width = height * AspectRatio;
        }

        return new Size(width, height);
    }
}