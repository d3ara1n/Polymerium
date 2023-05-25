using System;
using Humanizer;
using Microsoft.UI.Xaml.Data;

namespace Polymerium.App.Converters;

public class DateTimeOffsetToHumanStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTimeOffset time)
            return time.Humanize();
        return "从未";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
