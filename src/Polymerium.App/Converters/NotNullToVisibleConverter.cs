using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Polymerium.App.Converters
{
    internal class NotNullToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                null or "" => Visibility.Collapsed,
                _ => Visibility.Visible
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}