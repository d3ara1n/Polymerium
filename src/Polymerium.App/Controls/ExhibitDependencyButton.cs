using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Polymerium.App.Controls;

public class ExhibitDependencyButton : Button
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<ExhibitDependencyButton, bool>(nameof(IsChecked));

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }
}