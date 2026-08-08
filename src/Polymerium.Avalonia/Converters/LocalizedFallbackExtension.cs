using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Polymerium.Avalonia.Converters;

// NOTE: Irihi.Lingua 的 {Translate} 返回的是 BindingBase（绑定对象），不是字符串，塞进
//  TargetNullValue/FallbackValue 这种静态值槽会落成桥接对象的 FullName，所以「主值 or 本地化兜底」
public sealed class LocalizedFallbackExtension : MarkupExtension
{
    public LocalizedFallbackExtension() { }

    public LocalizedFallbackExtension(BindingBase source) => Source = source;

    public BindingBase? Source { get; set; }

    public BindingBase? Fallback { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Source is null)
        {
            throw new InvalidOperationException("LocalizedFallback requires a source binding.");
        }

        var multi = new MultiBinding { Converter = CoalesceConverter.Instance };
        multi.Bindings.Add(Source);
        if (Fallback is not null)
        {
            multi.Bindings.Add(Fallback);
        }

        return multi;
    }

    private sealed class CoalesceConverter : IMultiValueConverter
    {
        public static readonly CoalesceConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value == AvaloniaProperty.UnsetValue)
                {
                    continue;
                }

                if (value is string s)
                {
                    if (s.Length > 0)
                    {
                        return s;
                    }
                }
                else if (value is not null)
                {
                    return value;
                }
            }

            return null;
        }
    }
}
