using Avalonia;

namespace Polymerium.App.Controls;

public class PackageEntryEnabilityFilter : AvaloniaObject
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<PackageEntryEnabilityFilter, string>(nameof(Label));

    public static readonly StyledProperty<bool?> EnabilityProperty =
        AvaloniaProperty.Register<PackageEntryEnabilityFilter, bool?>(nameof(Enability));

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool? Enability
    {
        get => GetValue(EnabilityProperty);
        set => SetValue(EnabilityProperty, value);
    }
}