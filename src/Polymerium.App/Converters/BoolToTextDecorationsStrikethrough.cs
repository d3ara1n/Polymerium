﻿using Microsoft.UI.Xaml.Data;
using System;
using Windows.UI.Text;

namespace Polymerium.App.Converters
{
    public class BoolToTextDecorationsStrikethrough : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value switch
            {
                true => TextDecorations.None,
                false => TextDecorations.Strikethrough,
                _ => TextDecorations.None
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
