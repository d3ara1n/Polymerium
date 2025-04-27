using Avalonia;

namespace Huskui.Avalonia.Models;

public class LazyObject(
    Func<CancellationToken, Task<object?>> factory,
    Action<object?>? callback = null,
    CancellationToken token = default) : AvaloniaObject
{
    private readonly CancellationTokenSource _cts = CancellationTokenSource.CreateLinkedTokenSource(token);

    public static readonly DirectProperty<LazyObject, object?> ValueProperty =
        AvaloniaProperty.RegisterDirect<LazyObject, object?>(nameof(Value), o => o.Value, (o, v) => o.Value = v);

    public object? Value
    {
        get;
        private set => SetAndRaise(ValueProperty, ref field, value);
    }

    public bool IsCancelled => _cts.IsCancellationRequested;

    public static readonly DirectProperty<LazyObject, bool> IsInProgressProperty =
        AvaloniaProperty.RegisterDirect<LazyObject, bool>(nameof(IsInProgress),
                                                          o => o.IsInProgress,
                                                          (o, v) => o.IsInProgress = v);

    public bool IsInProgress
    {
        get;
        private set => SetAndRaise(IsInProgressProperty, ref field, value);
    }


    public Action<object?>? Callback { get; set; } = callback;
    public void Cancel() => _cts.Cancel();

    public async Task FetchAsync()
    {
        IsInProgress = true;
        var value = await factory(_cts.Token);
        Value = value;
        IsInProgress = false;
        Callback?.Invoke(value);
    }
}