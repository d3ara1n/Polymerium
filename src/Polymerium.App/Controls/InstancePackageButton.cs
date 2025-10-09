using Avalonia;
using Avalonia.Controls;

namespace Polymerium.App.Controls;

public class InstancePackageButton : Button
{
    public static readonly StyledProperty<bool> IsRefreshingProperty =
        AvaloniaProperty.Register<InstancePackageButton, bool>(nameof(IsRefreshing));

    public bool IsRefreshing
    {
        get => GetValue(IsRefreshingProperty);
        set => SetValue(IsRefreshingProperty, value);
    }
}
