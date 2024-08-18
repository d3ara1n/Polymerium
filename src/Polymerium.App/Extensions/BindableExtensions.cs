using Polymerium.App.Models;
using System;
using System.Collections.Generic;

namespace Polymerium.App.Extensions;

public static class BindableExtensions
{
    public static Bindable<TOwner, TValue> ToBindable<TOwner, TValue>(this TOwner self, Func<TOwner, TValue> getter,
        Action<TOwner, TValue> setter) where TOwner : class =>
        new(self, getter, setter);

    public static BindableCollection<T> ToBindableCollection<T>(this IList<T> self) => new(self);

    public static Reactive<TOwner, TSource, TValue> ToReactive<TOwner, TSource, TValue>(
        this Bindable<TOwner, TSource> self, Func<TSource, TValue> selector) where TOwner : class =>
        new(self, selector);

    public static ReactiveCollection<TSource, TValue> ToReactiveCollection<TSource, TValue>(this IList<TSource> self,
        Func<TSource, TValue> selector, Func<TValue, TSource> mapper) =>
        new(self, selector, mapper);
}