using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.App.Controls;

public class PackageEntryEnabilityFilter : AvaloniaObject
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<PackageEntryEnabilityFilter, string>(nameof(Label));

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly StyledProperty<bool?> EnabilityProperty =
        AvaloniaProperty.Register<PackageEntryEnabilityFilter, bool?>(nameof(Enability));

    public bool? Enability
    {
        get => GetValue(EnabilityProperty);
        set => SetValue(EnabilityProperty, value);
    }
}