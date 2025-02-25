using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;

namespace Huskui.Avalonia.Models;

public class LazyObject(Func<CancellationToken, Task<object?>> factory, CancellationToken token = default)
    : INotifyPropertyChanged
{
    private Exception? _error;
    private bool _isInitialized;
    private bool _isInProgress;
    private bool _isSuccessful;
    private object? _value;

    public CancellationTokenSource CancellationTokenSource { get; } =
        CancellationTokenSource.CreateLinkedTokenSource(token);

    public object? Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    public Exception? Error
    {
        get => _error;
        set => SetField(ref _error, value);
    }

    public bool IsInitialized
    {
        get => _isInitialized;
        set => SetField(ref _isInitialized, value);
    }

    public bool IsSuccessful
    {
        get => _isSuccessful;
        set => SetField(ref _isSuccessful, value);
    }

    public bool IsInProgress
    {
        get => _isInProgress;
        set => SetField(ref _isInProgress, value);
    }

    public void StartInitialize() => Task.Run(StartInitialize);

    public async Task InitializeAsync()
    {
        if (IsInProgress || CancellationTokenSource.Token.IsCancellationRequested)
            return;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsInProgress = true);
            var obj = await factory(CancellationTokenSource.Token);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Value = obj;
                IsInitialized = true;
                IsSuccessful = true;
                IsInProgress = false;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Error = ex;
                IsInitialized = true;
                IsSuccessful = false;
                IsInProgress = false;
            });
        }
    }

    #region Property Changed

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}