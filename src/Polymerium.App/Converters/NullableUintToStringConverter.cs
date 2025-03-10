using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Polymerium.App.Converters;

public class NullableUintToStringConverter : IValueConverter
{
    public static readonly NullableUintToStringConverter Instance = new NullableUintToStringConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            null => string.Empty,
            uint ui => ui.ToString(),
            _ => value
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            string str when string.IsNullOrEmpty(str) => null,
            string str when uint.TryParse(str, out var ui) => ui,
            _ => value
        };
}