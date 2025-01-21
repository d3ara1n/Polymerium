using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_Indicator, typeof(Arc))]
[PseudoClasses(":indeterminate")]
public class ProgressRing : RangeBase
{
    public const string PART_Indicator = nameof(PART_Indicator);

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<ProgressRing, bool>(nameof(IsIndeterminate));

    public static readonly StyledProperty<bool> ShowProgressTextProperty =
        AvaloniaProperty.Register<ProgressRing, bool>(nameof(ShowProgressText));

    public static readonly StyledProperty<string> ProgressTextFormatProperty =
        AvaloniaProperty.Register<ProgressRing, string>(nameof(ProgressTextFormat), "{1:0}%");

    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(StrokeWidth), 4);


    public static readonly DirectProperty<ProgressRing, double> PercentageProperty =
        AvaloniaProperty.RegisterDirect<ProgressRing, double>(
            nameof(Percentage),
            o => o.Percentage);

    private Arc? _indicator;

    private double _percentage;

    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public bool ShowProgressText
    {
        get => GetValue(ShowProgressTextProperty);
        set => SetValue(ShowProgressTextProperty, value);
    }

    public string ProgressTextFormat
    {
        get => GetValue(ProgressTextFormatProperty);
        set => SetValue(ProgressTextFormatProperty, value);
    }

    public double StrokeWidth
    {
        get => GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public double Percentage
    {
        get => _percentage;
        private set => SetAndRaise(PercentageProperty, ref _percentage, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsIndeterminateProperty) PseudoClasses.Set(":indeterminate", change.GetNewValue<bool>());

        if (change.Property == ValueProperty ||
            change.Property == MinimumProperty ||
            change.Property == MaximumProperty ||
            change.Property == IsIndeterminateProperty)
            UpdateIndicator();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _indicator = e.NameScope.Find<Arc>(PART_Indicator);
        UpdateIndicator();
    }

    private void UpdateIndicator()
    {
        if (_indicator == null) return;

        var percent = Math.Abs(Maximum - Minimum) < double.Epsilon ? 1.0 : (Value - Minimum) / (Maximum - Minimum);

        Percentage = percent * 100;

        var angle = percent * 360;
        angle %= 360;

        _indicator.SweepAngle = angle;
    }
}