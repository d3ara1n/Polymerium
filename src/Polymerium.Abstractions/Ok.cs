namespace Polymerium.Abstractions;

public class Ok<TOk, TErr> : Result<TOk, TErr>
{
    public Ok(TOk value)
    {
        Value = value;
    }

    public TOk Value { get; }
}

public class Ok<TErr> : Result<TErr>
{
}
