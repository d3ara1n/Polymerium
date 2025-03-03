using Avalonia.Data.Converters;
using System.Globalization;

namespace Huskui.Avalonia.Converters;

public class RelayMultiConverter(Func<IList<object?>, object?, CultureInfo, object?> convert) : IMultiValueConverter
{
    #region IMultiValueConverter Members

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        convert(values, parameter, culture);

    #endregion
}