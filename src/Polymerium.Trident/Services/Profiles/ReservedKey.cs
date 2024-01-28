namespace Polymerium.Trident.Services.Profiles;

// RAII, 但是即使被 dispose 了依旧能传递给 ProfileManager.Append
public record ReservedKey : IDisposable
{
    private readonly ProfileManager _parent;

    internal ReservedKey(ProfileManager parent, string key)
    {
        _parent = parent;
        Key = key;
    }

    public string Key { get; }
    public bool Disposed { get; private set; }

    public void Dispose()
    {
        if (Disposed) return;
        Disposed = true;
        _parent.ReleaseKey(this);
    }
}