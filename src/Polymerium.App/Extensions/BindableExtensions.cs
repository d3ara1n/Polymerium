using Polymerium.App.Models;
using System;
using System.Collections.Generic;

namespace Polymerium.App.Extensions;

public static class BindableExtensions
{
    public static Bindable<TOwner, TValue> ToBindable<TOwner, TValue>(this TOwner self, Func<TOwner, TValue> getter,
        Action<TOwner, TValue> setter)
        where TOwner : class
    {
        return new Bindable<TOwner, TValue>(self, getter, setter);
    }

    public static BindableCollection<T> ToBindableCollection<T>(this IList<T> self)
    {
        return new BindableCollection<T>(self);
    }

    public static Reactive<TOwner, TSource, TValue> ToReactive<TOwner, TSource, TValue>(
        this Bindable<TOwner, TSource> self,
        Func<TSource, TValue> selector)
        where TOwner : class
    {
        return new Reactive<TOwner, TSource, TValue>(self, selector);
    }

    public static ReactiveCollection<TSource, TValue> ToReactiveCollection<TSource, TValue>(
        this IList<TSource> self,
        Func<TSource, TValue> selector, Func<TValue, TSource> mapper)
    {
        return new ReactiveCollection<TSource, TValue>(self, selector, mapper);
    }
}