using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Polymerium.Avalonia.Converters;

// NOTE: 本地化枚举的 key 约定为 {EnumType.Name}_{Value}（如 ResourceKind_Mod），
// 新增枚举本地化只需在 resx 补对应 key，无需改此 Converter 或 XAML。
file sealed class LocalizedEnumConverter : IMultiValueConverter
{
    public static readonly LocalizedEnumConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0 || values[0] is not Enum e) return values.ElementAtOrDefault(0);
        var key = $"{e.GetType().Name}_{e}";
        return LanguageManager.Instance.GetObservable(key) is { } o ? o.Current() : e;
    }
}

public sealed class LocalizedEnumExtension : MarkupExtension
{
    public LocalizedEnumExtension() { }

    public LocalizedEnumExtension(BindingBase source) => Source = source;

    public BindingBase? Source { get; set; }

    public object? FallbackValue { get; set; }

    public object? TargetNullValue { get; set; }

    public override object ProvideValue(IServiceProvider sp)
    {
        var mb = new MultiBinding
        {
            Converter = LocalizedEnumConverter.Instance,
            FallbackValue = FallbackValue,
            TargetNullValue = TargetNullValue
        };
        mb.Bindings.Add(Source!);
        mb.Bindings.Add(LanguageManager.Instance.CultureChanges.ToBinding());
        return mb;
    }
}
