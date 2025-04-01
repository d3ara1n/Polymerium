using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Huskui.Avalonia.Models;

namespace Huskui.Avalonia.Controls;

public class LazyContainer : ContentControl
{
    public static readonly StyledProperty<object?> BadContentProperty =
        AvaloniaProperty.Register<LazyContainer, object?>(nameof(BadContent));

    public static readonly DirectProperty<LazyContainer, LazyObject?> LazySourceProperty =
        AvaloniaProperty.RegisterDirect<LazyContainer, LazyObject?>(nameof(LazySource),
                                                                    o => o.LazySource,
                                                                    (o, v) => o.LazySource = v);

    public static readonly StyledProperty<bool> IsBadProperty =
        AvaloniaProperty.Register<LazyContainer, bool>(nameof(IsBad));

    public object? BadContent
    {
        get => GetValue(BadContentProperty);
        set => SetValue(BadContentProperty, value);
    }

    public LazyObject? LazySource
    {
        get;
        set => SetAndRaise(LazySourceProperty, ref field, value);
    }

    public bool IsBad
    {
        get => GetValue(IsBadProperty);
        set => SetValue(IsBadProperty, value);
    }


    protected override async void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LazySourceProperty)
        {
            if (change.OldValue is LazyObject { IsCancelled: false } old)
                old.Cancel();
            Content = null;
            IsBad = false;
            if (change.NewValue is LazyObject lazy)
                try
                {
                    Content = await lazy.FetchAsync().ConfigureAwait(true);
                }
                catch
                {
                    IsBad = true;
                }
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (LazySource is { IsCancelled: false })
            LazySource.Cancel();
    }
}