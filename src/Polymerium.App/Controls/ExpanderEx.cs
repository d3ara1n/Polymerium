using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;

namespace Polymerium.App.Controls;

public class ExpanderEx : HeaderedContentControl
{
    // Using a DependencyProperty as the backing store for IsOpen.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded),
        typeof(bool),
        typeof(ExpanderEx),
        new PropertyMetadata(false, IsExpanded_Changed)
    );

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    private static void IsExpanded_Changed(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args
    )
    {
        var expander = sender as ExpanderEx;
        VisualStateManager.GoToState(expander, expander!.IsExpanded ? "Open" : "Normal", true);
    }
}
