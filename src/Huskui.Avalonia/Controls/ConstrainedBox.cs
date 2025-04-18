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

        var desired = new Size(width, height);
        // if (Content is Control control)
        // {
        //     control.Measure(new Size(width, height));
        //     desired = control.DesiredSize;
        // }
        // 答案是不关心内部成员大小，不 Measure 他们！

        return desired;
    }
}