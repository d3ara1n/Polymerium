using Avalonia.Interactivity;
using Huskui.Avalonia.Controls;

namespace Polymerium.Avalonia.Sidebars;

public partial class NotificationSidebar : Sidebar
{
    public NotificationSidebar() => InitializeComponent();

    private void DismissButton_OnClick(object? sender, RoutedEventArgs e) =>
        RaiseEvent(new OverlayHost.DismissRequestedEventArgs(this));
}
