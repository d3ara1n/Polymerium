using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Models
{
    public class SynchronizedMappedCollection<I, O> : ObservableCollection<O>
    {
        private readonly IList<I> innerList;
        private readonly Func<I, O> toFunc;
        private readonly Func<O, I> fromFunc;

        public SynchronizedMappedCollection(IList<I> inner, Func<O, I> from, Func<I, O> to)
            : base(inner.Select(to))
        {
            innerList = inner;
            toFunc = to;
            fromFunc = from;
        }

        protected override void InsertItem(int index, O item)
        {
            innerList.Insert(index, fromFunc(item));
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            innerList.RemoveAt(index);
            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            innerList.Clear();
            base.ClearItems();
        }

        protected override void SetItem(int index, O item)
        {
            innerList[index] = fromFunc(item);
            base.SetItem(index, item);
        }
    }
}