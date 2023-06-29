using Microsoft.UI.Xaml.Data;
using System;

namespace Polymerium.App.Converters;

public class NullToParameterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
            return parameter;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
