using Avalonia;
using Avalonia.Controls;

namespace Polymerium.Avalonia.Controls;

public class DependencyGraphCard : Button
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<DependencyGraphCard, bool>(nameof(IsSelected));

    public static readonly StyledProperty<bool> IsMissingProperty =
        AvaloniaProperty.Register<DependencyGraphCard, bool>(nameof(IsMissing));

    static DependencyGraphCard()
    {
        IsSelectedProperty.Changed.AddClassHandler<DependencyGraphCard>((card, e) =>
            card.PseudoClasses.Set(SELECTED_CLASS, (bool)e.NewValue!));
        IsMissingProperty.Changed.AddClassHandler<DependencyGraphCard>((card, e) =>
            card.PseudoClasses.Set(MISSING_CLASS, (bool)e.NewValue!));
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsMissing
    {
        get => GetValue(IsMissingProperty);
        set => SetValue(IsMissingProperty, value);
    }

    private const string SELECTED_CLASS = ":selected";
    private const string MISSING_CLASS = "missing";
}
