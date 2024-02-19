using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Polymerium.App.Models
{
    public class Bindable<TOwner, TValue>(TOwner owner, Func<TOwner, TValue> getter, Action<TOwner, TValue> setter)
        : INotifyPropertyChanged
        where TOwner : class
    {
        public TValue Value
        {
            get => getter(owner);
            set
            {
                TValue old = Value;
                if (!EqualityComparer<TValue>.Default.Equals(old, value))
                {
                    setter(owner, value);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}