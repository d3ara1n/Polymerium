using FluentIcons.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Controls;
public class Label : Control
{


    public FluentIcons.Common.Symbol Icon
    {
        get { return (FluentIcons.Common.Symbol)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Icon.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(FluentIcons.Common.Symbol), typeof(Label), new PropertyMetadata(FluentIcons.Common.Symbol.Accessibility));





    public IconVariant Variant
    {
        get { return (IconVariant)GetValue(VariantProperty); }
        set { SetValue(VariantProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Variant.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(nameof(Variant), typeof(IconVariant), typeof(Label), new PropertyMetadata(IconVariant.Regular));


    public double Spacing
    {
        get { return (double)GetValue(SpacingProperty); }
        set { SetValue(SpacingProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Spacing.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SpacingProperty =
        DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(Label), new PropertyMetadata(0));




    public string Text
    {
        get { return (string)GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(Label), new PropertyMetadata(string.Empty));




    public TextTrimming Trimming
    {
        get { return (TextTrimming)GetValue(TrimmingProperty); }
        set { SetValue(TrimmingProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Trimming.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TrimmingProperty =
        DependencyProperty.Register(nameof(Trimming), typeof(TextTrimming), typeof(Label), new PropertyMetadata(TextTrimming.None));


}