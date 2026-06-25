using Avalonia;
using Avalonia.Controls;
using FluentIcons.Common;

namespace Polymerium.Avalonia.Controls;

// Supplies a label and icon for a navigable section that is not a SettingsEntry
// (which already exposes its own Title/Icon) — e.g. the About panel.
public sealed class NavigationSectionProperties
{
    private NavigationSectionProperties() { }

    public static readonly AttachedProperty<string?> TitleProperty =
        AvaloniaProperty.RegisterAttached<NavigationSectionProperties, Control, string?>("Title");

    public static readonly AttachedProperty<Symbol> IconProperty =
        AvaloniaProperty.RegisterAttached<NavigationSectionProperties, Control, Symbol>("Icon");

    public static string? GetTitle(Control element) => element.GetValue(TitleProperty);

    public static void SetTitle(Control element, string? value) => element.SetValue(TitleProperty, value);

    public static Symbol GetIcon(Control element) => element.GetValue(IconProperty);

    public static void SetIcon(Control element, Symbol value) => element.SetValue(IconProperty, value);
}
