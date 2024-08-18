using System.Collections.ObjectModel;

namespace Polymerium.App.Models;

public class LimitedCollection<T>(int limit) : ObservableCollection<T>
{
    public int Limit => limit;

    protected override void InsertItem(int index, T item)
    {
        base.InsertItem(index, item);
        if (Count > limit)
        {
            RemoveAt(0);
        }
    }
}