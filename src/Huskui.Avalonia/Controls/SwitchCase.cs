using Avalonia;
using Avalonia.Metadata;

namespace Huskui.Avalonia.Controls;

public class SwitchCase : AvaloniaObject
{
    public static readonly DirectProperty<SwitchCase, object> ContentProperty =
        AvaloniaProperty.RegisterDirect<SwitchCase, object>(nameof(Content), o => o.Content, (o, v) => o.Content = v);

    public static readonly DirectProperty<SwitchCase, object> ValueProperty =
        AvaloniaProperty.RegisterDirect<SwitchCase, object>(nameof(Value), o => o.Value, (o, v) => o.Value = v);

    private object _content = null!;

    private object _value = null!;

    [Content]
    public object Content
    {
        get => _content;
        set => SetAndRaise(ContentProperty, ref _content, value);
    }

    public object Value
    {
        get => _value;
        set => SetAndRaise(ValueProperty, ref _value, value);
    }
}