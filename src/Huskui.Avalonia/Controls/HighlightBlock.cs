using Avalonia;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls;

// 为什么是 TemplatedControl 而不是 Inline，因为 Inline 没有样式，但应该像 Inline 那样被使用
public class HighlightBlock : TemplatedControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<HighlightBlock, string>(nameof(Text));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}