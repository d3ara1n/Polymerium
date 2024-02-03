﻿using System;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Data;

namespace Polymerium.App.Converters;

public class BoolToFontStyleStrikethroughConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? TextDecorations.Strikethrough : TextDecorations.None;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}