using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Polymerium.Avalonia.Converters;

// NOTE: 本地化枚举的 key 约定为 {EnumType.Name}_{Value}（如 ResourceKind_Mod），
// 新增枚举本地化只需在 resx 补对应 key，无需改此 Converter 或 XAML。
public sealed class LocalizedEnumExtension : MarkupExtension
{
    public LocalizedEnumExtension() { }

    public LocalizedEnumExtension(BindingBase source) => Source = source;

    public BindingBase? Source { get; set; }

    public object FallbackValue { get; set; } = AvaloniaProperty.UnsetValue;

    public object TargetNullValue { get; set; } = AvaloniaProperty.UnsetValue;

    public override object ProvideValue(IServiceProvider sp)
    {
        if (Source is null) throw new InvalidOperationException("LocalizedEnum requires a source binding.");
        var target = (sp.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget)?.TargetObject as StyledElement;
        return LocalizedTextSource.Create(target, Source, FallbackValue, TargetNullValue);
    }
}
