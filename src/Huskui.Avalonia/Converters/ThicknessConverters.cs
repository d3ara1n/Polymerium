using Avalonia;
using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

public static class ThicknessConverters
{
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