using Microsoft.UI.Xaml.Data;
using System;

namespace Polymerium.App.Converters;

public class BoolToAccentTextBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is true &&
            App.Current.Resources.TryGetValue("AccentTextFillColorPrimaryBrush", out var result))
        {
            return result;
        }

        if (App.Current.Resources.TryGetValue("ApplicationForegroundThemeBrush", out var def))
        {
            return def;
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}