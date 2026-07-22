using Avalonia;
using Avalonia.Controls;

namespace Polymerium.Avalonia.Controls;

public class DependencyGraphCard : Button
{
    private const string CLASS_SELECTED = ":selected";
    private const string CLASS_MISSING = ":missing";

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<DependencyGraphCard, bool>(nameof(IsSelected));

    public static readonly StyledProperty<bool> IsMissingProperty =
        AvaloniaProperty.Register<DependencyGraphCard, bool>(nameof(IsMissing));

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsSelectedProperty)
        {
            PseudoClasses.Set(CLASS_SELECTED, change.GetNewValue<bool>());
        }

        if (change.Property == IsMissingProperty)
        {
            PseudoClasses.Set(CLASS_MISSING, change.GetNewValue<bool>());
        }
    }
}
