using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Polymerium.App.Models;

public class ReactiveCollection<TSource, TValue>(
    IList<TSource> from,
    Func<TSource, TValue> selector,
    Func<TValue, TSource> mapper)
    : Collection<TValue>(from.Select(selector).ToList()), INotifyCollectionChanged, INotifyPropertyChanged
{
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    protected override void ClearItems()
    {
        base.ClearItems();

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionReset();
    }

    protected override void RemoveItem(int index)
    {
        var removedItem = this[index];


        from.Remove(mapper(removedItem));
        base.RemoveItem(index);

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
    }

    protected override void InsertItem(int index, TValue item)
    {
        var value = mapper(item);
        from.Insert(index, value);
        base.InsertItem(index, item);

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
    }

    protected override void SetItem(int index, TValue item)
    {
        var originalItem = this[index];
        from[index] = mapper(item);
        base.SetItem(index, item);

        OnIndexerPropertyChanged();
        OnCollectionSet(item, originalItem, index);
    }

    private void OnCollectionChanged(NotifyCollectionChangedAction action, TValue item, int index) =>
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(action, item, index));

    private void OnCollectionSet(TValue add, TValue old, int index) =>
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, add, old, index));

    private void OnCollectionReset() =>
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void OnCountPropertyChanged() => OnPropertyChanged("Count");

    private void OnIndexerPropertyChanged() => OnPropertyChanged("Item[]");
}