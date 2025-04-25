namespace Huskui.Avalonia.Models;

public class LazyObject(
    Func<CancellationToken, Task<object?>> factory,
    Action<object?>? callback = null,
    CancellationToken token = default)
{
    private readonly CancellationTokenSource _cts = CancellationTokenSource.CreateLinkedTokenSource(token);

    public bool IsCancelled => _cts.IsCancellationRequested;
    public bool InProgress { get; private set; }
    public object? Value { get; private set; }
    public void Cancel() => _cts.Cancel();

    public Action<object?>? Callback { get; set; } = callback;

    public async Task FetchAsync()
    {
        InProgress = true;
        var value = await factory(_cts.Token).ConfigureAwait(true);
        Value = value;
        InProgress = false;
        Callback?.Invoke(value);
    }
}