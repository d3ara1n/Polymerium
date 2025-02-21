using Avalonia.Data.Converters;
using Avalonia.Input;

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

    public static IValueConverter KeyGestureToString { get; } = new RelayConverter((v, _) =>
    {
        return v switch
        {
            null => null,
            KeyGesture gesture => gesture.ToString("p", null),
            _ => throw new NotSupportedException()
        };
    });

    public static IValueConverter TrueIfMatch { get; } = new RelayConverter((v, p) => v == p);

    public static IValueConverter TrueIfNotMatch { get; } = new RelayConverter((v, p) => v != p);

    public static IValueConverter CountToArray { get; } = new RelayConverter((v, _) =>
    {
        if (v is int count) return Enumerable.Range(0, count).ToArray();

        return v;
    });
}