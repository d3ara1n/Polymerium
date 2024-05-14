using Microsoft.UI.Xaml.Data;

namespace Polymerium.App.Converters
{
    public class StringUppercaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value?.ToString()?.ToUpper() ?? parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}