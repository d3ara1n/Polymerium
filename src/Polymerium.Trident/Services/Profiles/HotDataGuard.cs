namespace Polymerium.Trident.Services.Profiles;

public class HotDataGuard<T> : IDisposable
{
    public T Value { get; }

    internal Action Callback;

    internal int RefCount;

    internal HotDataGuard(T value) => Value = value;

    public void Dispose() => Callback();
}