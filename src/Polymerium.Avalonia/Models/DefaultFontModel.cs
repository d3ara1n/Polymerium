using Avalonia.Media;

namespace Polymerium.Avalonia.Models;

// 用默认字体（未自定义）。Raw 为空串，Preview 为 fallback。
public sealed class DefaultFontModel(FontFamily fallback) : FontModelBase(fallback, true)
{
    public override string Raw => string.Empty;
}
