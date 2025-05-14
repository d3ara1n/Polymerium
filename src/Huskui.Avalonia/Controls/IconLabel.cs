using Avalonia;
using Avalonia.Controls.Primitives;
using FluentIcons.Common;

namespace Huskui.Avalonia.Controls;

public class IconLabel : TemplatedControl
{
    public static readonly StyledProperty<Symbol> IconProperty =
        AvaloniaProperty.Register<IconLabel, Symbol>(nameof(Icon));

    public Symbol Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<IconLabel, string>(nameof(Text));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<IconVariant> VariantProperty =
        AvaloniaProperty.Register<IconLabel, IconVariant>(nameof(Variant));

    public IconVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }


    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<IconLabel, double>(nameof(Spacing), 4d);

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }
}