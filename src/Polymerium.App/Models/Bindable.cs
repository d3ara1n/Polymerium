using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.App.Models;

public class Bindable<TOwner, TValue>(TOwner owner, Func<TOwner, TValue> getter, Action<TOwner, TValue> setter)
    : ObservableObject
    where TOwner : class
{
    public TValue Value
    {
        get => getter(owner);
        set => SetProperty(getter(owner), value, owner, setter);
    }
}