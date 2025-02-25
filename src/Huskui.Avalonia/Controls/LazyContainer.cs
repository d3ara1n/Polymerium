using Avalonia;
using Avalonia.Controls;
using Huskui.Avalonia.Models;

namespace Huskui.Avalonia.Controls;

public class LazyContainer : ContentControl
{
    public static readonly DirectProperty<LazyContainer, LazyObject?> LazyProperty =
        AvaloniaProperty.RegisterDirect<LazyContainer, LazyObject?>(nameof(Lazy), o => o.Lazy, (o, v) => o.Lazy = v);

    private LazyObject? _lazy;

    public LazyObject? Lazy
    {
        get => _lazy;
        set => SetAndRaise(LazyProperty, ref _lazy, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LazyProperty)
            if (change.NewValue is LazyObject
                {
                    IsInProgress: false,
                    IsInitialized: false,
                    CancellationTokenSource.IsCancellationRequested: false
                } lazy)
                lazy.StartInitialize();
    }
}