using System;
using Microsoft.UI.Xaml.Data;
using Polymerium.App.Models;

namespace Polymerium.App.Converters;

public class FalseWhenLoadingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DataLoadingState state) return state != DataLoadingState.Loading;

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}