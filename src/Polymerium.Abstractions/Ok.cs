namespace Polymerium.Abstractions
{
    public class Ok<TOk, TErr> : Result<TOk, TErr>
    {
        public TOk Value { get; private set; }

        public Ok(TOk value)
        {
            Value = value;
        }
    }

    public class Ok<TErr> : Result<TErr>
    { }
}