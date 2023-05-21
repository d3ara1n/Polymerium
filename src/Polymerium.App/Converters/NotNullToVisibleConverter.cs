using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Polymerium.App.Converters;

internal class NotNullToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, string language)
    {
        return value switch
        {
            null or "" => parameter == null ? Visibility.Collapsed : Visibility.Visible,
            _ => parameter == null ? Visibility.Visible : Visibility.Collapsed
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}