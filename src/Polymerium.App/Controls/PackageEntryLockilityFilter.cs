using Avalonia;

namespace Polymerium.App.Controls;

public class PackageEntryLockilityFilter : AvaloniaObject
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<PackageEntryLockilityFilter, string>(nameof(Label));

    public static readonly StyledProperty<bool?> LockilityProperty =
        AvaloniaProperty.Register<PackageEntryLockilityFilter, bool?>(nameof(Lockility));

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool? Lockility
    {
        get => GetValue(LockilityProperty);
        set => SetValue(LockilityProperty, value);
    }
}
