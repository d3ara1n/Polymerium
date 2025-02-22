using System.Globalization;
using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

public class RelayMultiConverter(Func<IList<object?>, object?, CultureInfo, object?> convert) : IMultiValueConverter
{
    #region IMultiValueConverter Members

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) => convert(values, parameter, culture);

    #endregion
}