namespace Huskui.Avalonia.Models;

public class LazyObject(Func<CancellationToken, Task<object?>> factory, CancellationToken token = default)
{
    private readonly CancellationTokenSource _cts = CancellationTokenSource.CreateLinkedTokenSource(token);

    public bool IsCancelled => _cts.IsCancellationRequested;
    public object? Value { get; private set; }
    public void Cancel() => _cts.Cancel();

    public async Task FetchAsync()
    {
        var value = await factory(_cts.Token).ConfigureAwait(false);
        Value = value;
    }
}