using Avalonia;
using Avalonia.Controls;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Controls;

public class HiddenModCard : ContentControl
{
    public const string CLASS_InnerDuplicate = ":duplicate-inner";
    public const string CLASS_TopLevelDuplicate = ":duplicate-toplevel";

    public static readonly StyledProperty<HiddenModEntry.DuplicateKind> DuplicateProperty =
        AvaloniaProperty.Register<HiddenModCard, HiddenModEntry.DuplicateKind>(nameof(Duplicate));

    public HiddenModEntry.DuplicateKind Duplicate
    {
        get => GetValue(DuplicateProperty);
        set => SetValue(DuplicateProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DuplicateProperty)
        {
            var value = change.GetNewValue<HiddenModEntry.DuplicateKind>();
            PseudoClasses.Set(CLASS_InnerDuplicate, value == HiddenModEntry.DuplicateKind.Inner);
            PseudoClasses.Set(CLASS_TopLevelDuplicate, value == HiddenModEntry.DuplicateKind.WithTopLevel);
        }
    }
}
