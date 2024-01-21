using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Polymerium.App.Models;

public class BindableCollection<T>(IList<T> from) : IList<T>, INotifyCollectionChanged
{
    public IEnumerator<T> GetEnumerator()
    {
        return from.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return from.GetEnumerator();
    }

    public void Add(T item)
    {
        from.Add(item);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
    }

    public void Clear()
    {
        from.Clear();
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null, -1));
    }

    public bool Contains(T item)
    {
        return from.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        from.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        var index = from.IndexOf(item);
        if (from.Remove(item))
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));

        return false;
    }

    public int Count => from.Count;
    public bool IsReadOnly => from.IsReadOnly;

    public int IndexOf(T item)
    {
        return from.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        from.Insert(index, item);
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, from.Count);
        var item = from[index];
        from.RemoveAt(index);
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
    }

    public T this[int index]
    {
        get => from[index];
        set
        {
            var old = from[index];
            from[index] = value;
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old));
        }
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
}