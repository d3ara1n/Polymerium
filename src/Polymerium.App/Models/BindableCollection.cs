using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Polymerium.App.Models;

public class BindableCollection<T>(IList<T> from)
    : Collection<T>(from), INotifyCollectionChanged, INotifyPropertyChanged
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

        base.RemoveItem(index);

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
    }

    protected override void InsertItem(int index, T item)
    {
        base.InsertItem(index, item);

        OnCountPropertyChanged();
        OnIndexerPropertyChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
    }

    protected override void SetItem(int index, T item)
    {
        var originalItem = this[index];
        base.SetItem(index, item);

        OnIndexerPropertyChanged();
        OnCollectionSet(item, originalItem, index);
    }

    protected virtual void MoveItem(int oldIndex, int newIndex)
    {
        var removedItem = this[oldIndex];

        base.RemoveItem(oldIndex);
        base.InsertItem(newIndex, removedItem);

        OnIndexerPropertyChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Move, removedItem, newIndex, oldIndex);
    }

    private void OnCollectionChanged(NotifyCollectionChangedAction action, T item, int index)
    {
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(action, item, index));
    }

    private void OnCollectionChanged(NotifyCollectionChangedAction action, T item, int newIndex, int oldIndex)
    {
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(action, item, newIndex, oldIndex));
    }

    private void OnCollectionSet(T add, T old, int index)
    {
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, add, old, index));
    }

    private void OnCollectionReset()
    {
        CollectionChanged?.Invoke(this,
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnCountPropertyChanged()
    {
        OnPropertyChanged(nameof(Count));
    }

    private void OnIndexerPropertyChanged()
    {
        OnPropertyChanged("Item[]");
    }
}