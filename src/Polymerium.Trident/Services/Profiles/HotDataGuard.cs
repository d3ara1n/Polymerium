namespace Polymerium.Trident.Services.Profiles;

public class HotDataGuard<T> : IDisposable
{
    internal Action Callback;

    internal int RefCount;

    internal HotDataGuard(T value) => Value = value;
    public T Value { get; }

    public void Dispose() => Callback();
}