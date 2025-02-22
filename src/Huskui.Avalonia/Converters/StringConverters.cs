using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

public static class StringConverters
{
    public static IValueConverter Match { get; } = new RelayConverter((v, p) =>
    {
        return p switch
        {
            string it => Equals(it, v),
            _ => false
        };
    });

    public static IValueConverter IsEmpty { get; } = new RelayConverter((v, _) =>
    {
        if (v is string str)
            return string.IsNullOrEmpty(str);

        return false;
    });

    public static IValueConverter IsNonEmpty { get; } = new RelayConverter((v, _) =>
    {
        if (v is string str)
            return !string.IsNullOrEmpty(str);

        return false;
    });

    public static IValueConverter ToUpper { get; } = new RelayConverter((v, _) => v?.ToString()?.ToUpper() ?? v);


    public static IValueConverter ToLower { get; } = new RelayConverter((v, _) => v?.ToString()?.ToLower() ?? v);
}