namespace Polymerium.Abstractions;

public class Err<TOk, TErr> : Result<TOk, TErr>
{
    public Err(TErr err)
    {
        Value = err;
    }

    public TErr Value { get; }
}

public class Err<TErr> : Result<TErr>
{
    public Err(TErr err)
    {
        Value = err;
    }

    public TErr Value { get; }
}