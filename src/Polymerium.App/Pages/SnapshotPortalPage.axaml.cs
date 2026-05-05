using System;
using Avalonia.Interactivity;
using Huskui.Avalonia.Controls;

using Page = Huskui.Avalonia.Controls.Page;

namespace Polymerium.App.Pages;

public partial class SnapshotPortalPage : Page
{
    public SnapshotPortalPage()
    {
        InitializeComponent();
    }

    private void OnCreateSnapshotClick(object? sender, RoutedEventArgs e)
    {
    }

    private void OnViewSnapshotsClick(object? sender, RoutedEventArgs e)
    {
        var current = Parent;
        while (current != null)
        {
            if (current is Frame frame)
            {
                frame.Navigate(typeof(SnapshotManagementPage), null, null);
                return;
            }

            current = current.Parent;
        }
    }
}
