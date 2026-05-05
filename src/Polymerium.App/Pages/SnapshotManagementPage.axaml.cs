using Avalonia.Controls;
using Avalonia.Interactivity;
using Huskui.Avalonia.Controls;

using Page = Huskui.Avalonia.Controls.Page;

namespace Polymerium.App.Pages;

public partial class SnapshotManagementPage : Page
{
    public SnapshotManagementPage()
    {
        InitializeComponent();
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        var current = Parent;
        while (current != null)
        {
            if (current is Frame frame)
            {
                frame.GoBack();
                return;
            }

            current = current.Parent;
        }
    }

    private void OnSnapshotSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SnapshotList.SelectedItem is not null)
        {
            DetailPanel.IsVisible = true;
            DetailEmptyHint.IsVisible = false;
        }
        else
        {
            DetailPanel.IsVisible = false;
            DetailEmptyHint.IsVisible = true;
        }
    }
}
