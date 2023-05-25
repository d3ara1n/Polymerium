using System.Collections.ObjectModel;

namespace Polymerium.Core.Stars.Facilities;

public class StreamBuffer<T> : ObservableCollection<T>
{
    public StreamBuffer()
        : this(999) { }

    public StreamBuffer(uint capacity)
    {
        Capacity = capacity;
    }

    public uint Capacity { get; }

    protected override void InsertItem(int index, T item)
    {
        base.InsertItem(index, item);
        if (Count > Capacity)
            RemoveAt(0);
    }
}
