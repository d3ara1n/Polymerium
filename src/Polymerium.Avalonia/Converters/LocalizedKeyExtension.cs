using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Polymerium.Avalonia.Converters;

// NOTE: 按字符串 key 查当前语言的值；查不到（如品牌名等字面）原样返回。
// 用于 POCO/ViewModel 持有字符串 key、XAML 绑定显示并随语言热切换的场景。
public sealed class LocalizedKeyExtension : MarkupExtension
{
    public LocalizedKeyExtension() { }

    public LocalizedKeyExtension(BindingBase source) => Source = source;

    public BindingBase? Source { get; set; }

    public object FallbackValue { get; set; } = AvaloniaProperty.UnsetValue;

    public object TargetNullValue { get; set; } = AvaloniaProperty.UnsetValue;

    public override object ProvideValue(IServiceProvider sp)
    {
        if (Source is null) throw new InvalidOperationException("LocalizedKey requires a source binding.");
        var target = (sp.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget)?.TargetObject as StyledElement;
        return LocalizedTextSource.Create(target, Source, FallbackValue, TargetNullValue);
    }
}
