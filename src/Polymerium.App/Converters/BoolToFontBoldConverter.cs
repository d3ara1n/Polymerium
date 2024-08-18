using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Data;
using System;

namespace Polymerium.App.Converters;

public class BoolToFontBoldConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? FontWeights.Bold : FontWeights.Normal;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}