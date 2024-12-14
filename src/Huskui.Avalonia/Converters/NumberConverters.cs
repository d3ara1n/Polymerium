using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

public static class NumberConverters
{
    public static IValueConverter IsZero { get; } = new RelayConverter((v, _) => IsObjectZero(v));

    public static IValueConverter IsNonZero { get; } = new RelayConverter((v, _) => !IsObjectZero(v));

    private static bool IsObjectZero(object? value)
    {
        if (value is int i) return i == 0;
        if (value is long l) return l == 0;
        if (value is float f) return f == 0;
        if (value is double d) return d == 0;
        if (value is string s && double.TryParse(s, out var o)) return o == 0;

        return false;
    }
}