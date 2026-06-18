using SkiaSharp;

namespace Polymerium.Avalonia.Rendering;

/// <summary>
///     识别一张皮肤位图的格式。
/// </summary>
public static class SkinFormat
{
    /// <summary>
    ///     按尺寸与特定区域透明度判定皮肤格式。
    /// </summary>
    public static SkinType Detect(SKBitmap skin)
    {
        if (skin.Width >= 64 && skin.Height >= 64 && skin.Width == skin.Height)
            return IsSlim(skin) ? SkinType.Slim : SkinType.Classic;

        if (skin.Width == skin.Height * 2)
            return SkinType.Legacy;

        // 非标准尺寸按经典尽力渲染。
        return SkinType.Classic;
    }

    // slim 皮肤在手臂外层第 4 列像素区域应为完全透明（classic 占满 4 像素宽）。
    // 坐标基于 64×64 模板，按实际宽度缩放。
    private static bool IsSlim(SKBitmap skin)
    {
        var s = skin.Width / 64;
        return IsBlank(skin, 50 * s, 16 * s, 2 * s, 4 * s)
            && IsBlank(skin, 54 * s, 20 * s, 2 * s, 12 * s)
            && IsBlank(skin, 42 * s, 48 * s, 2 * s, 4 * s)
            && IsBlank(skin, 46 * s, 52 * s, 2 * s, 12 * s);
    }

    private static bool IsBlank(SKBitmap skin, int x, int y, int w, int h)
    {
        for (var i = 0; i < w; i++)
            for (var j = 0; j < h; j++)
                if (skin.GetPixel(x + i, y + j).Alpha != 0)
                    return false;
        return true;
    }
}
