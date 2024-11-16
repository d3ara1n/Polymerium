using System.Globalization;
using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

public class RelayConverter(Func<object?, object?, object?> convert) : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return convert(value, parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}