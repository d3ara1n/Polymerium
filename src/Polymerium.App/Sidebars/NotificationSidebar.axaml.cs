using Avalonia.Interactivity;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Sidebars;

public partial class NotificationSidebar : Sidebar
{
    public NotificationSidebar() => InitializeComponent();

    private void DimissButton_OnClick(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));
    }
}
