using Huskui.Avalonia;

namespace Polymerium.Avalonia.Models;

/// <summary>
///     调色板预设的展示模型：名称、预设本体，以及用于预览卡片的三组代表色（暗色表面、强调色、暗色前景文字）。
/// </summary>
public record PalettePreviewItem(string Name, ColorPalette Palette, string AccentHex, string DarkSurfaceHex, string TextHex)
{
    public static readonly PalettePreviewItem[] All =
    [
        new(nameof(ColorPalette.Neutral), ColorPalette.Neutral, "#6E6E6E", "#191919", "#EEEEEC"),
        new(nameof(ColorPalette.Ocean), ColorPalette.Ocean, "#0090FF", "#18191B", "#EDEEF0"),
        new(nameof(ColorPalette.Forest), ColorPalette.Forest, "#33B074", "#171918", "#ECEEED"),
        new(nameof(ColorPalette.Sakura), ColorPalette.Sakura, "#AB4ABA", "#1A191B", "#EEEEF0"),
        new(nameof(ColorPalette.Ember), ColorPalette.Ember, "#E8A855", "#121110", "#F5F0EB"),
        new(nameof(ColorPalette.Honey), ColorPalette.Honey, "#FFC53D", "#191918", "#EEEEEC"),
        new(nameof(ColorPalette.Matcha), ColorPalette.Matcha, "#BDEE63", "#181917", "#ECEEEC"),
    ];
}
