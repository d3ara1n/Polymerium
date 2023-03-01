using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Polymerium.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, string language)
    {
        if (value is bool b)
            return (parameter == null ? b : !b) ? Visibility.Visible : Visibility.Collapsed;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}