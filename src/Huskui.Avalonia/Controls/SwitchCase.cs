using Avalonia;
using Avalonia.Metadata;

namespace Huskui.Avalonia.Controls;

public class SwitchCase : AvaloniaObject
{
    public static readonly DirectProperty<SwitchCase, object?> ContentProperty =
        AvaloniaProperty.RegisterDirect<SwitchCase, object?>(nameof(Content), o => o.Content, (o, v) => o.Content = v);

    public static readonly DirectProperty<SwitchCase, object?> ValueProperty =
        AvaloniaProperty.RegisterDirect<SwitchCase, object?>(nameof(Value), o => o.Value, (o, v) => o.Value = v);

    [Content]
    public object? Content
    {
        get;
        set => SetAndRaise(ContentProperty, ref field, value);
    }

    public object? Value
    {
        get;
        set => SetAndRaise(ValueProperty, ref field, value);
    } = AvaloniaProperty.UnsetValue;
}