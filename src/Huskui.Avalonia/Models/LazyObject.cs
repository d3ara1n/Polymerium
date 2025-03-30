namespace Huskui.Avalonia.Models;

public class LazyObject(Func<CancellationToken, Task<object?>> factory, CancellationToken token = default)
{
    private readonly CancellationTokenSource _cts = CancellationTokenSource.CreateLinkedTokenSource(token);

    public bool IsCancelled => _cts.IsCancellationRequested;
    public void Cancel() => _cts.Cancel();
    public async Task<object?> FetchAsync() => await factory(_cts.Token).ConfigureAwait(false);
}