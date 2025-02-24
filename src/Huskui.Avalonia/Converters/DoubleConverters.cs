using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

public static class DoubleConverters
{
    public static IValueConverter DividedBy { get; } = new RelayConverter((v, p) =>
    {
        if (v is double o)
        {
            if (p is double d)
                return o / d;

            if (p is int i)
                return o / i;

            if (p is string s && double.TryParse(s, out var r))
            {
                var l = o / r;
                return l > 1 ? l : 1;
            }
        }

        return v;
    });
}