using Avalonia;
using Avalonia.Controls;

namespace Polymerium.Avalonia.Controls;

public class DependencyGraphCard : Button
{
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
            PseudoClasses.Set(SELECTED_CLASS, change.GetNewValue<bool>());
        }

        if (change.Property == IsMissingProperty)
        {
            PseudoClasses.Set(MISSING_CLASS, change.GetNewValue<bool>());
        }
    }

    private const string SELECTED_CLASS = ":selected";
    private const string MISSING_CLASS = ":missing";
}
