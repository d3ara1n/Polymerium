using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Polymerium.App.Controls;

public class GlowingButton : ButtonBase
{
    // Using a DependencyProperty as the backing store for GlowColor.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty GlowColorProperty = DependencyProperty.Register(
        nameof(GlowColor),
        typeof(Color),
        typeof(GlowingButton),
        new PropertyMetadata(null)
    );

    public GlowingButton()
    {
        IsEnabledChanged += GlowingButton_IsEnabledChanged;
    }

    public Color GlowColor
    {
        get => (Color)GetValue(GlowColorProperty);
        set => SetValue(GlowColorProperty, value);
    }

    private void GlowingButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue?.ToString()?.ToLower() == "true")
            VisualStateManager.GoToState(this, "Normal", true);
        else
            VisualStateManager.GoToState(this, "Disabled", true);
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "Pressed", true);
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "Normal", true);
        base.OnPointerReleased(e);
    }
}