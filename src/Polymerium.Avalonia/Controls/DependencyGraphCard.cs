using Avalonia;
using Avalonia.Controls;

namespace Polymerium.Avalonia.Controls;

public class DependencyGraphCard : Button
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<DependencyGraphCard, bool>(nameof(IsSelected));

    static DependencyGraphCard()
    {
        IsSelectedProperty.Changed.AddClassHandler<DependencyGraphCard>((card, e) =>
            card.PseudoClasses.Set(SELECTED_CLASS, (bool)e.NewValue!));
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    private const string SELECTED_CLASS = ":selected";
}
