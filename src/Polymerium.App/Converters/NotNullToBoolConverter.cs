using Microsoft.UI.Xaml.Data;
using System;

namespace Polymerium.App.Converters;

public class NotNullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return parameter == null ? value != null : value == null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}