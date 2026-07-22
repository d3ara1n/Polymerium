using Avalonia;
using Avalonia.Controls;

namespace Polymerium.Avalonia.Controls;

public class PackageDependencyButton : Button
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<PackageDependencyButton, bool>(nameof(IsChecked));

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }
}
