using System.Globalization;
using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

public class RelayMultiConverter(Func<IList<object?>, object?, CultureInfo, object?> convert) : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        return convert(values, parameter, culture);
    }
}