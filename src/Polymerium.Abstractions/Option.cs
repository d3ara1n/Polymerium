using System;

namespace Polymerium.Abstractions;

public abstract class Option<T>
{
    private static readonly None<T> none = new();

    public static None<T> None()
    {
        return none;
    }

    public static Some<T> Some(T data)
    {
        return new Some<T>(data);
    }

    public bool IsSome()
    {
        return this is Some<T>;
    }

    public bool IsNone()
    {
        return !IsSome();
    }

    public static Option<T> Wrap(T value)
    {
        return value switch
        {
            null => None(),
            _ => Some(value!)
        };
    }

    public bool TryUnwrap(out T? data)
    {
        if (this is Some<T> some)
        {
            data = some.Value;
            return true;
        }

        data = default;
        return false;
    }

    public T Unwrap()
    {
        if (this is Some<T> some)
            return some.Value;
        throw new NullReferenceException(typeof(T).FullName);
    }
}