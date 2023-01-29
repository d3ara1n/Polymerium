namespace Polymerium.Abstractions
{
    public class Some<T> : Option<T>
    {
        public T Value { get; private set; }

        public Some(T data)
        {
            Value = data;
        }
    }
}