using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Styling;

namespace Polymerium.Avalonia.Converters;

// NOTE: 动态 key 的本地化绑定直接订阅 key 对应的 observable，而非在 CultureChanges 触发时读一次 Current()。
// 生成的 UpdateCulture 先推 CultureChanges、后更新各 key observable；以 CultureChanges 为触发器读 Current()
// 会让绑定永远显示上一语言且不自愈。订阅 key observable 后 CultureChanges 的触发顺序无关紧要——key 更新即推送。
// 未知 key（如品牌名字面）原样返回。
internal sealed class LocalizedTextSource : StyledElement
{
    public static readonly StyledProperty<object?> KeyProperty =
        AvaloniaProperty.Register<LocalizedTextSource, object?>(nameof(Key));

    public object? Key
    {
        get => GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    public static BindingBase Create(StyledElement? target, BindingBase source, object fallbackValue, object targetNullValue)
    {
        var keyBinding = new MultiBinding
        {
            Converter = KeyConverter.Instance,
            FallbackValue = fallbackValue,
            TargetNullValue = targetNullValue
        };
        keyBinding.Bindings.Add(source);

        return Observable.Create<object?>(observer =>
        {
            var helper = new LocalizedTextSource();
            var subs = new CompositeDisposable();
            subs.Add(helper.Bind(KeyProperty, keyBinding));
            if (target is not null)
            {
                subs.Add(target.GetObservable(StyledElement.DataContextProperty)
                               .Subscribe(dc => helper.DataContext = dc));
            }

            subs.Add(helper.GetObservable(KeyProperty)
                           .Select(Resolve)
                           .Switch()
                           .Subscribe(observer));
            return subs;
        }).ToBinding();
    }

    private static IObservable<object?> Resolve(object? raw)
    {
        if (raw == AvaloniaProperty.UnsetValue) return Observable.Return<object?>(AvaloniaProperty.UnsetValue);
        var key = raw switch
        {
            null => null,
            Enum e => $"{e.GetType().Name}_{e}",
            _ => raw as string ?? raw.ToString()
        };

        if (key is null) return Observable.Return<object?>(null);
        return LanguageManager.Instance.GetObservable(key) is { } observable
            ? observable
            : Observable.Return(raw);
    }

    private sealed class KeyConverter : IMultiValueConverter
    {
        public static readonly KeyConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values.Count > 0 ? values[0] : null;
    }
}
