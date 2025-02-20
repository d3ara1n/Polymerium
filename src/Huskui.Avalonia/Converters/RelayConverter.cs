﻿using Avalonia.Data.Converters;
using System.Globalization;

namespace Huskui.Avalonia.Converters;

public class RelayConverter(Func<object?, object?, object?> convert) : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        convert(value, parameter);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}