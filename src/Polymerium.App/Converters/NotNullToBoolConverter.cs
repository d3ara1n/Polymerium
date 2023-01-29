using Microsoft.UI.Xaml.Data;
using System;

namespace Polymerium.App.Converters
{
    internal class NotNullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                null or "" => false,
                _ => true
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}