using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Modals;

public partial class PackageEntryModal : Modal
{
    public PackageEntryModal() => InitializeComponent();

    public static readonly DirectProperty<PackageEntryModal, InstancePackageModel> ModelProperty =
        AvaloniaProperty.RegisterDirect<PackageEntryModal, InstancePackageModel>(nameof(Model),
            o => o.Model,
            (o, v) => o.Model = v);

    public required InstancePackageModel Model
    {
        get;
        set => SetAndRaise(ModelProperty, ref field, value);
    }


    private void Source_Click(object? sender, RoutedEventArgs e)
    {
        if (Model is { Reference: not null } model)
            TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync(model.Reference);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        // Discard ProfileGuard
    }
}