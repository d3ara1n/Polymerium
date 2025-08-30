using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Polymerium.App.Models;

public class MappingCollection<TSource, TValue>(
    IList<TSource> from,
    Func<TSource, TValue> mapper,
    Func<TValue, TSource> selector) : ObservableCollection<TValue>([.. from.Select(mapper)])
{
    protected override void ClearItems()
    {
        from.Clear();
        base.ClearItems();
    }

    protected override void RemoveItem(int index)
    {
        var removedItem = this[index];


        from.Remove(selector(removedItem));
        base.RemoveItem(index);
    }

    protected override void InsertItem(int index, TValue item)
    {
        var value = selector(item);
        from.Insert(index, value);
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, TValue item)
    {
        from[index] = selector(item);
        base.SetItem(index, item);
    }
}
