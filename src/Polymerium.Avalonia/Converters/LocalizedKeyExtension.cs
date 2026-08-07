using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Polymerium.Avalonia.Converters;

// NOTE: 按字符串 key 查当前语言的值；查不到（如品牌名等字面）原样返回。
// 用于 POCO/ViewModel 持有字符串 key、XAML 绑定显示并随语言热切换的场景。
// values[1](CultureChanges) 纯作触发。
file sealed class LocalizedKeyConverter : IMultiValueConverter
{
    public static readonly LocalizedKeyConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0 || values[0] is not string s) return values.ElementAtOrDefault(0);
        return LanguageManager.Instance.GetObservable(s) is { } o ? o.Current() : s;
    }
}

public sealed class LocalizedKeyExtension : MarkupExtension
{
    public LocalizedKeyExtension() { }

    public LocalizedKeyExtension(BindingBase source) => Source = source;

    public BindingBase? Source { get; set; }

    public object? FallbackValue { get; set; }

    public object? TargetNullValue { get; set; }

    public override object ProvideValue(IServiceProvider sp)
    {
        var mb = new MultiBinding
        {
            Converter = LocalizedKeyConverter.Instance,
            FallbackValue = FallbackValue,
            TargetNullValue = TargetNullValue
        };
        mb.Bindings.Add(Source!);
        mb.Bindings.Add(LanguageManager.Instance.CultureChanges.ToBinding());
        return mb;
    }
}
