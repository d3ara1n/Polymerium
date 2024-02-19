using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Polymerium.App.Converters
{
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool val = value switch
            {
                int it => it == 0,
                uint it => it == 0,
                long it => it == 0,
                _ => false
            };
            if (parameter != null)
            {
                val = !val;
            }

            return val
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}