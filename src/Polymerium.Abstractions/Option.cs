using System;

namespace Polymerium.Abstractions
{
    public abstract class Option<T>
    {
        private static None<T> none = new None<T>();

        public static None<T> None() => none;

        public static Some<T> Some(T data) => new Some<T>(data);

        public bool IsSome() => this is Some<T>;

        public bool IsNone() => !IsSome();

        public static Option<T> Wrap(T? value) => value switch
        {
            null => None(),
            _ => Some(value!)
        };

        public bool TryUnwrap(out T data)
        {
            if (this is Some<T> some)
            {
                data = some.Value;
                return true;
            }
            else
            {
                data = default;
                return false;
            }
        }

        public T Unwrap()
        {
            if (this is Some<T> some)
            {
                return some.Value;
            }
            else
            {
                throw new NullReferenceException(typeof(T).FullName);
            }
        }
    }
}