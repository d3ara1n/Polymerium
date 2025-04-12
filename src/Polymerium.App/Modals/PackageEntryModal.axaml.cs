using Avalonia.Controls;
using Avalonia.Interactivity;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
using Polymerium.Trident.Services.Profiles;

namespace Polymerium.App.Modals;

public partial class PackageEntryModal : Modal
{
    public PackageEntryModal() => InitializeComponent();

    public required ProfileGuard Guard { get; init; }

    private InstancePackageModel Model => (DataContext as InstancePackageModel)!;

    private void Source_Click(object? sender, RoutedEventArgs e)
    {
        if (Model is { Reference: not null } model)
            TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync(model.Reference);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
    }

    private void DismissButton_Click(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));
    }

    private void RemoveVersionButton_Click(object? sender, RoutedEventArgs e)
    {
        Model.Version = new InstancePackageUnspecifiedVersionModel();
    }
}