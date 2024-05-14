using Microsoft.UI.Xaml.Data;

namespace Polymerium.App.Converters
{
    public class BoolToTranslucentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is true ? 1.0 : 0.5;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}