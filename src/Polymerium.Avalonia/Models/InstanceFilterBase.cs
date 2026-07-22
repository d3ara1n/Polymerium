using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.Avalonia.Models;

public abstract class InstanceFilterBase : ObservableObject, IDisposable
{
    public abstract string Label { get; }

    public abstract ReadOnlyObservableCollection<FilterOptionModel> Options { get; }

    public abstract IObservable<Func<InstanceCardModel, bool>> Predicate { get; }

    public abstract bool IsActive { get; protected set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public abstract void Clear();

    protected virtual void Dispose(bool disposing) { }
}
