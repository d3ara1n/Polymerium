namespace Polymerium.Abstractions;

public class Some<T> : Option<T>
{
    public Some(T data)
    {
        Value = data;
    }

    public T Value { get; }
}