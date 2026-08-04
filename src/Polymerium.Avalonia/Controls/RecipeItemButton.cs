using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Polymerium.Avalonia.Controls;

public class RecipeItemButton : Button
{
    public static readonly DirectProperty<RecipeItemButton, ICommand?> RemoveCommandProperty =
        AvaloniaProperty.RegisterDirect<RecipeItemButton, ICommand?>(nameof(RemoveCommand),
                                                                      o => o.RemoveCommand,
                                                                      (o, v) => o.RemoveCommand = v);

    public ICommand? RemoveCommand
    {
        get;
        set => SetAndRaise(RemoveCommandProperty, ref field, value);
    }
}
