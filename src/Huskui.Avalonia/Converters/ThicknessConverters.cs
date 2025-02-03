using Avalonia;
using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

public static class ThicknessConverters
{
    public static IValueConverter WithoutBottom { get; } = new RelayConverter((v, _) => v switch
    {
        Thickness it => new Thickness(it.Left, it.Top, it.Right, 0d),
        _ => v
    });

    public static IValueConverter WithoutTop { get; } = new RelayConverter((v, _) => v switch
    {
        Thickness it => new Thickness(it.Left, 0d, it.Right, it.Bottom),
        _ => v
    });

    public static IValueConverter WithoutLeft { get; } = new RelayConverter((v, _) => v switch
    {
        Thickness it => new Thickness(0d, it.Top, it.Right, it.Left),
        _ => v
    });

    public static IValueConverter WithoutRight { get; } = new RelayConverter((v, _) => v switch
    {
        Thickness it => new Thickness(it.Left, it.Top, 0d, it.Bottom),
        _ => v
    });

    public static IValueConverter ToDouble { get; } = new RelayConverter((v, p) =>
    {
        if (v is Thickness thickness)
            return p?.ToString()?.ToLower() switch
            {
                "top" => thickness.Top,
                "right" => thickness.Right,
                "bottom" => thickness.Bottom,
                "left" => thickness.Left,
                _ => thickness
            };

        return v;
    });
}