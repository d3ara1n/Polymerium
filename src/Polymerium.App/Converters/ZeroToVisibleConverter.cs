using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Polymerium.App.Converters;

public class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            int it => it == 0,
            long it => it == 0,
            double it => Math.Abs(it) < 0.001,
            float it => Math.Abs(it) < 0.001,
            uint it => it == 0,
            ulong it => it == 0,
            _ => false
        }
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}