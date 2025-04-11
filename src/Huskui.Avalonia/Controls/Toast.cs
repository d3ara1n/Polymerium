using Avalonia;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls;

public class Toast : HeaderedContentControl
{
    public static readonly StyledProperty<bool> IsHeaderVisibleProperty =
        AvaloniaProperty.Register<Toast, bool>(nameof(IsHeaderVisible), true);

    public bool IsHeaderVisible
    {
        get => GetValue(IsHeaderVisibleProperty);
        set => SetValue(IsHeaderVisibleProperty, value);
    }

    public void Dismiss()
    {
        RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));
    }

    protected override Type StyleKeyOverride { get; } = typeof(Toast);
}