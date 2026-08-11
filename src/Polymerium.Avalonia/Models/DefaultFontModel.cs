using Avalonia.Media;

namespace Polymerium.Avalonia.Models;

// NOTE: 未自定义时用默认字体——Raw 为空串，Preview 为 fallback。
public sealed class DefaultFontModel(FontFamily fallback) : FontModelBase(fallback, true)
{
    public override string Raw => string.Empty;
}
