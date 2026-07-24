using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Polymerium.Avalonia.Controls;

public class SettingsEntryItem : HeaderedContentControl
{
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingsEntryItem, string?>(nameof(Description));

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<SettingsEntryItem, Orientation>(nameof(Orientation));

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }
}
