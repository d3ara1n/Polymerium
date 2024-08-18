using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Polymerium.App.Converters;

public class NotEmptyToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        var res = value switch
        {
            string it => !string.IsNullOrEmpty(it),
            IEnumerable<object> it => it.Any(),
            IEnumerable it => it.Cast<object>().Any(),
            _ => value != null
        };
        return (parameter == null ? res : !res) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}