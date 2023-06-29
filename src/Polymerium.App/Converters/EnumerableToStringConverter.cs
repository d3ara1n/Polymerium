using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;

namespace Polymerium.App.Converters;

public class EnumerableToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            IEnumerable<string> list
                => string.Join(parameter != null ? parameter.ToString() : ", ", list),
            _ => value
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
