using Avalonia;
using Avalonia.Controls;
using FluentIcons.Common;

namespace Polymerium.App.Controls;

public class EmptyContainer : ContentControl
{
    public static readonly StyledProperty<Symbol> IconProperty =
        AvaloniaProperty.Register<EmptyContainer, Symbol>(nameof(Icon), Symbol.List);

    public static readonly StyledProperty<bool> IsEmptyProperty =
        AvaloniaProperty.Register<EmptyContainer, bool>(nameof(IsEmpty));

    public Symbol Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsEmpty
    {
        get => GetValue(IsEmptyProperty);
        set => SetValue(IsEmptyProperty, value);
    }
}
