using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.App.Dialogs;

public partial class VersionPickerDialog : Dialog
{
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private Func<CancellationToken, Task<IReadOnlyList<GameVersionModel>>> _factory;

    public static readonly DirectProperty<VersionPickerDialog, ObservableCollection<GameVersionModel>> ItemsProperty =
        AvaloniaProperty.RegisterDirect<VersionPickerDialog, ObservableCollection<GameVersionModel>>(nameof(Items),
            o => o.Items,
            (o, v) => o.Items = v);

    private ObservableCollection<GameVersionModel> _items;

    public ObservableCollection<GameVersionModel> Items
    {
        get => _items;
        set => SetAndRaise(ItemsProperty, ref _items, value);
    }


    public VersionPickerDialog(Func<CancellationToken, Task<IReadOnlyList<GameVersionModel>>> factory)
    {
        _factory = factory;
        InitializeComponent();
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            var index = await _factory(_cts.Token);
            // TODO: 使用 SourceCache 去 AddRange();
        }
        catch (Exception ex)
        {
            // TODO: show error content
        }
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _cts.Cancel();
    }
}