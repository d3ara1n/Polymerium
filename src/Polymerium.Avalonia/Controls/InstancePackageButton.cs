using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Polymerium.Avalonia.Controls;

public class InstancePackageButton : Button
{
    public static readonly DirectProperty<InstancePackageButton, ICommand?> EditCommandProperty =
        AvaloniaProperty.RegisterDirect<InstancePackageButton, ICommand?>(nameof(EditCommand),
                                                                          o => o.EditCommand,
                                                                          (o, v) => o.EditCommand = v);

    public ICommand? EditCommand
    {
        get;
        set => SetAndRaise(EditCommandProperty, ref field, value);
    }

    public static readonly DirectProperty<InstancePackageButton, ICommand?> RemoveCommandProperty =
        AvaloniaProperty.RegisterDirect<InstancePackageButton, ICommand?>(nameof(RemoveCommand),
                                                                          o => o.RemoveCommand,
                                                                          (o, v) => o.RemoveCommand = v);

    public ICommand? RemoveCommand
    {
        get;
        set => SetAndRaise(RemoveCommandProperty, ref field, value);
    }

    public static readonly DirectProperty<InstancePackageButton, ICommand?> RefreshCommandProperty =
        AvaloniaProperty.RegisterDirect<InstancePackageButton, ICommand?>(nameof(RefreshCommand),
                                                                          o => o.RefreshCommand,
                                                                          (o, v) => o.RefreshCommand = v);

    public ICommand? RefreshCommand
    {
        get;
        set => SetAndRaise(RefreshCommandProperty, ref field, value);
    }
}
