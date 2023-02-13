using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Polymerium.App.Models;

public class SynchronizedCollection<T> : ObservableCollection<T>
{
    private readonly IList<T> inner;

    public SynchronizedCollection(IList<T> from)
        : base(from)
    {
        inner = from;
    }

    protected override void InsertItem(int index, T item)
    {
        inner.Insert(index, item);
        base.InsertItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        inner.RemoveAt(index);
        base.RemoveItem(index);
    }

    protected override void ClearItems()
    {
        inner.Clear();
        base.ClearItems();
    }

    protected override void SetItem(int index, T item)
    {
        inner[index] = item;
        base.SetItem(index, item);
    }
}
