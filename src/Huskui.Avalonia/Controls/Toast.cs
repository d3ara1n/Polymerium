using Avalonia;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls;

public class Toast : HeaderedContentControl
{
    // public static readonly DirectProperty<Toast, string> TitleProperty =
    //     AvaloniaProperty.RegisterDirect<Toast, string>(nameof(Title), o => o.Title, (o, v) => o.Title = v);
    //
    // public string Title
    // {
    //     get;
    //     set => SetAndRaise(TitleProperty, ref field, value);
    // } = string.Empty;

    public static readonly StyledProperty<bool> IsHeaderVisibleProperty =
        AvaloniaProperty.Register<Toast, bool>(nameof(IsHeaderVisible), true);

    public bool IsHeaderVisible
    {
        get => GetValue(IsHeaderVisibleProperty);
        set => SetValue(IsHeaderVisibleProperty, value);
    }


    protected override Type StyleKeyOverride { get; } = typeof(Toast);
}