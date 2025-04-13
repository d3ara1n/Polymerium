using Avalonia.Interactivity;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Modals;

public partial class ExhibitPackageModal : Modal
{
    public ExhibitPackageModal()
    {
        InitializeComponent();
    }

    private void DismissButton_OnClick(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));
    }
}