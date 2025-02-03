using Avalonia;
using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

public static class CornerRadiusConverters
{
    public static IValueConverter Upper { get; } = new RelayConverter((v, _) => v switch
    {
        CornerRadius it => new CornerRadius(it.TopLeft, it.TopRight, 0d, 0d),
        _ => v
    });

    public static IValueConverter Lower { get; } = new RelayConverter((v, _) => v switch
    {
        CornerRadius it => new CornerRadius(0d, 0d, it.BottomRight, it.BottomLeft),
        _ => v
    });

    public static IValueConverter Left { get; } = new RelayConverter((v, _) => v switch
    {
        CornerRadius it => new CornerRadius(it.TopLeft, 0d, 0d, it.BottomLeft),
        _ => v
    });

    public static IValueConverter Right { get; } = new RelayConverter((v, _) => v switch
    {
        CornerRadius it => new CornerRadius(0d, it.TopRight, it.BottomRight, 0d),
        _ => v
    });

    public static IValueConverter ToDouble { get; } = new RelayConverter((v, p) =>
    {
        if (v is CornerRadius radius)
            return p?.ToString()?.ToLower() switch
            {
                "topright" => radius.TopRight,
                "topleft" => radius.TopLeft,
                "bottomright" => radius.BottomRight,
                "bottomleft" => radius.BottomLeft,
                _ => radius
            };

        return v;
    });
}