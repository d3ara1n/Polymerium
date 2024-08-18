using Microsoft.UI.Xaml.Data;
using System;

namespace Polymerium.App.Converters;

public class StringUppercaseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value?.ToString()?.ToUpper() ?? parameter;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}