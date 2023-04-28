using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Stars.Facilities
{
    public class StreamBuffer<T> : ObservableCollection<T>
    {
        public uint Capacity { get; }

        public StreamBuffer()
            : this(999) { }

        public StreamBuffer(uint capacity)
        {
            Capacity = capacity;
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            if (Count > Capacity)
            {
                RemoveAt(0);
            }
        }
    }
}
