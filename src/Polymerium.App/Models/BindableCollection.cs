using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Polymerium.App.Models;

public class BindableCollection<T>(IList<T> from) : ObservableCollection<T>(from)
{
    protected override void InsertItem(int index, T item)
    {
        from.Insert(index, item);
        base.InsertItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        from.RemoveAt(index);
        base.RemoveItem(index);
    }

    protected override void ClearItems()
    {
        from.Clear();
        base.ClearItems();
    }

    protected override void SetItem(int index, T item)
    {
        from[index] = item;
        base.SetItem(index, item);
    }
}