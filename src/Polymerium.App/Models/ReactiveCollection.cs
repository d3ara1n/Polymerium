using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Polymerium.App.Models;

public class ReactiveCollection<TSource, TValue> : IReadOnlyList<TValue>, INotifyCollectionChanged
{
    private readonly Func<TSource, TValue> _selector;
    private readonly BindableCollection<TSource> _source;

    public ReactiveCollection(BindableCollection<TSource> source, Func<TSource, TValue> selector)
    {
        _source = source;
        _selector = selector;
        source.CollectionChanged += SourceOnCollectionChanged;
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
    {
        return new ReactiveCollectionEnumerator(_source.GetEnumerator(), _selector);
    }

    public IEnumerator GetEnumerator()
    {
        return ((IEnumerable<TValue>)this).GetEnumerator();
    }

    public int Count => _source.Count;

    public TValue this[int index] => _selector(_source[index]);

    private void SourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var (changed, index) = e.Action switch
        {
            NotifyCollectionChangedAction.Add => (((IEnumerable<TSource>)e.NewItems!).Select(_selector),
                e.NewStartingIndex),
            NotifyCollectionChangedAction.Remove => (((IEnumerable<TSource>)e.OldItems!).Select(_selector),
                e.OldStartingIndex),
            NotifyCollectionChangedAction.Reset => (Enumerable.Empty<TValue>(), -1),
            _ => throw new NotImplementedException()
        };
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(e.Action, changed, index));
    }

    private class ReactiveCollectionEnumerator(IEnumerator<TSource> inner, Func<TSource, TValue> selector)
        : IEnumerator<TValue>
    {
        public bool MoveNext()
        {
            return inner.MoveNext();
        }

        public void Reset()
        {
            inner.Reset();
        }

        public TValue Current => selector(inner.Current);

        object? IEnumerator.Current => Current;

        public void Dispose()
        {
            inner.Dispose();
        }
    }
}