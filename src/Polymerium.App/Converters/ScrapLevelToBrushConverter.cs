using Microsoft.UI.Xaml.Data;
using Polymerium.Trident.Engines.Launching;
using System;

namespace Polymerium.App.Converters
{
    public class ScrapLevelToBrushConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ScrapLevel level)
            {
                return level switch
                {
                    ScrapLevel.Information => App.Current.Resources.TryGetValue(
                        parameter != null ? "SystemFillColorNeutralBackgroundBrush" : "SystemFillColorNeutralBrush",
                        out object? r)
                        ? r
                        : null,
                    ScrapLevel.Warning => App.Current.Resources.TryGetValue(
                        parameter != null ? "SystemFillColorCautionBackgroundBrush" : "SystemFillColorCautionBrush",
                        out object? r)
                        ? r
                        : null,
                    ScrapLevel.Error => App.Current.Resources.TryGetValue(
                        parameter != null ? "SystemFillColorCriticalBackgroundBrush" : "SystemFillColorCriticalBrush",
                        out object? r)
                        ? r
                        : null,
                    _ => null
                };
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}