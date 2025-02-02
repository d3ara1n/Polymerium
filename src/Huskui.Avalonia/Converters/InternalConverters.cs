using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

internal static class InternalConverters
{
    public static IValueConverter IntegerIfNull { get; } = new RelayConverter((v, p) =>
    {
        if (p is int) return v is null ? p : 0;

        if (p is string str && int.TryParse(str, out var i)) return v is null ? i : 0;

        return 0;
    });

    public static IMultiValueConverter StringFormat { get; } = new RelayMultiConverter((v, _, info) =>
    {
        if (v is [string format, ..]) return string.Format(info, format, v.Skip(1).ToArray());

        return v;
    });
}