using System.Globalization;
using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

public class RelayConverter(Func<object?, object?, object?> convert) : IValueConverter
{
    #region IValueConverter Members

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        convert(value, parameter);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();

    #endregion

    internal static object ConvertValue(Type targetType, object value)
    {
        if (targetType.IsInstanceOfType(value))
            return value;

        if (targetType.IsEnum && value is string str)
        {
            if (Enum.TryParse(targetType, str, out var result))
                return result;

            throw new InvalidOperationException("The requested enum value was not present in the provided type.");
        }

        return DefaultValueConverter.Instance.Convert(value, targetType, null, CultureInfo.CurrentCulture) ?? value;
    }

    internal static bool CompareValues(object? compare, object? value, Type? targetType)
    {
        if (compare == null || value == null)
            return compare == value;

        if (targetType == null || (targetType == compare.GetType() && targetType == value.GetType()))
            // Default direct object comparison or we're all the proper type
            return compare.Equals(value);

        if (compare.GetType() == targetType)
        {
            // If we have a TargetType and the first value is the right type
            // Then our 2nd value isn't, so convert to string and coerce.
            var valueBase2 = ConvertValue(targetType, value);

            return compare.Equals(valueBase2);
        }

        // Neither of our two values matches the type so
        // we'll convert both to a String and try and coerce it to the proper type.
        var compareBase = ConvertValue(targetType, compare);

        var valueBase = ConvertValue(targetType, value);

        return compareBase.Equals(valueBase);
    }
}