using Microsoft.UI.Xaml.Data;
using System;

namespace Polymerium.App.Converters;

public class ReverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            bool it => !it,
            _ => value
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
