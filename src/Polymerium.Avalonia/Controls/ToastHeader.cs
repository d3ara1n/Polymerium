using Avalonia;
using Huskui.Avalonia.Controls;

namespace Polymerium.Avalonia.Controls;

public sealed class ToastHeader : AvaloniaObject
{
    public static readonly AttachedProperty<double?> RevealOffsetProperty =
        AvaloniaProperty.RegisterAttached<ToastHeader, Toast, double?>("RevealOffset");

    public static double? GetRevealOffset(Toast toast) => toast.GetValue(RevealOffsetProperty);

    public static void SetRevealOffset(Toast toast, double? value) => toast.SetValue(RevealOffsetProperty, value);
}
