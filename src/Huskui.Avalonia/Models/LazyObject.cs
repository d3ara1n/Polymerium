using Avalonia;
using Avalonia.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Huskui.Avalonia.Models;

public class LazyObject(Func<CancellationToken, Task<object?>> factory, CancellationToken token = default)
    : INotifyPropertyChanged
{
    #region Property Changed

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    public CancellationTokenSource CancellationTokenSource { get; } =
        CancellationTokenSource.CreateLinkedTokenSource(token);

    public object? Value { get => _value; set => SetField(ref _value, value); }
    private object? _value;

    public Exception? Error { get => _error; set => SetField(ref _error, value); }
    private Exception? _error;

    public bool IsInitialized { get => _isInitialized; set => SetField(ref _isInitialized, value); }
    private bool _isInitialized;

    public bool IsSuccessful { get => _isSuccessful; set => SetField(ref _isSuccessful, value); }
    private bool _isSuccessful;

    public bool IsInProgress { get => _isInProgress; set => SetField(ref _isInProgress, value); }
    private bool _isInProgress;

    public void StartInitialize()
    {
        Task.Run(StartInitialize);
    }

    public async Task InitializeAsync()
    {
        if (IsInProgress || CancellationTokenSource.Token.IsCancellationRequested) return;
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
}