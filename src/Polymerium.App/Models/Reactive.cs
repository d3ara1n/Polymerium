using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;

namespace Polymerium.App.Models
{
    public class Reactive<TOwner, TSource, TValue>
        : ObservableObject
        where TOwner : class
    {
        private readonly Func<TSource, TValue> _selector;
        private readonly Bindable<TOwner, TSource> _source;

        public Reactive(Bindable<TOwner, TSource> source, Func<TSource, TValue> selector)
        {
            _source = source;
            _selector = selector;
            Value = selector(source.Value);

            _source.PropertyChanged += SourceOnPropertyChanged;
        }

        public TValue Value { get; private set; }

        private void SourceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Bindable<TOwner, TSource>.Value))
            {
                var changed = _source.Value;
                Value = _selector(changed);
                OnPropertyChanged(nameof(Value));
            }
        }
    }
}