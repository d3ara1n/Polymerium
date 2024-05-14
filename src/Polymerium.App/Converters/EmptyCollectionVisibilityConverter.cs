using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System.Collections;

namespace Polymerium.App.Converters
{
    public class EmptyCollectionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                IList<object> it => it.Count,
                IList it => it.Count,
                ICollection<object> it => it.Count,
                ICollection it => it.Count,
                _ => 114514
            } == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}