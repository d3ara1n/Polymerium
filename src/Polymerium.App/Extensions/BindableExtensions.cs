using System;
using Polymerium.App.Models;

namespace Polymerium.App.Extensions;

public static class BindableExtensions
{
    public static Reactive<TOwner, TSource, TValue> Observe<TOwner, TSource, TValue>(
        this Bindable<TOwner, TSource> self,
        Func<TSource, TValue> selector)
        where TOwner : class
    {
        return new Reactive<TOwner, TSource, TValue>(self, selector);
    }

    public static ReactiveCollection<TSource, TValue> Observe<TSource, TValue>(this BindableCollection<TSource> self,
        Func<TSource, TValue> selector)
    {
        return new ReactiveCollection<TSource, TValue>(self, selector);
    }
}