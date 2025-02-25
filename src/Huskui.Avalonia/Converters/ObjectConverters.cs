using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

public static class ObjectConverters
{
    public static IValueConverter Match { get; } =
        new RelayConverter((v, p) => RelayConverter.CompareValues(v, p, v?.GetType()));

    public static IValueConverter NotMatch { get; } =
        new RelayConverter((v, p) => !RelayConverter.CompareValues(v, p, v?.GetType()));
}