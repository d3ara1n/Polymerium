using FluentIcons.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Symbol = FluentIcons.Common.Symbol;

namespace Polymerium.App.Controls;

public class Label : Control
{
    // Using a DependencyProperty as the backing store for Icon.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(Symbol), typeof(Label),
            new PropertyMetadata(Symbol.Accessibility));

    // Using a DependencyProperty as the backing store for Variant.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(nameof(Variant), typeof(IconVariant), typeof(Label),
            new PropertyMetadata(IconVariant.Regular));

    // Using a DependencyProperty as the backing store for Spacing.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SpacingProperty =
        DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(Label), new PropertyMetadata(0));

    // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(Label), new PropertyMetadata(string.Empty));

    // Using a DependencyProperty as the backing store for Trimming.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TrimmingProperty =
        DependencyProperty.Register(nameof(Trimming), typeof(TextTrimming), typeof(Label),
            new PropertyMetadata(TextTrimming.None));


    public Symbol Icon
    {
        get => (Symbol)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }


    public IconVariant Variant
    {
        get => (IconVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }


    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }


    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }


    public TextTrimming Trimming
    {
        get => (TextTrimming)GetValue(TrimmingProperty);
        set => SetValue(TrimmingProperty, value);
    }
}