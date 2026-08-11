using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Polymerium.Avalonia.Converters;

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
