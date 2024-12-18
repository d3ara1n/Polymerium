using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

internal static class InternalConverters
{
    public static IValueConverter NullThenInteger { get; } = new RelayConverter((v, p) =>
    {
        if (p is int) return v is null ? p : 0;

        if (p is string str && int.TryParse(str, out var i)) return v is null ? i : 0;

        return 0;
    });
}