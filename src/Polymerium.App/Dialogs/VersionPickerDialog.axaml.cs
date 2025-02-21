using Avalonia;
using Avalonia.Interactivity;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.App.Dialogs;

public partial class VersionPickerDialog : Dialog
{
    private IDisposable? _subscription;
    private readonly SourceCache<GameVersionModel, string> _versions = new(x => x.Name);
    private readonly CancellationTokenSource _cts = new();
    private Func<CancellationToken, Task<IReadOnlyList<GameVersionModel>>> _factory;

    public static readonly DirectProperty<VersionPickerDialog, ReadOnlyObservableCollection<GameVersionModel>?>
        ViewProperty =
            AvaloniaProperty.RegisterDirect<VersionPickerDialog, ReadOnlyObservableCollection<GameVersionModel>?>(
                nameof(View), o => o.View, (o, v) => o.View = v);

    private ReadOnlyObservableCollection<GameVersionModel>? _view;

    public ReadOnlyObservableCollection<GameVersionModel>? View
    {
        get => _view;
        set => SetAndRaise(ViewProperty, ref _view, value);
    }

    public static readonly DirectProperty<VersionPickerDialog, string> FilterTextProperty =
        AvaloniaProperty.RegisterDirect<VersionPickerDialog, string>(nameof(FilterText), o => o.FilterText,
            (o, v) => o.FilterText = v);

    private string _filterText = string.Empty;

    public string FilterText
    {
        get => _filterText;
        set => SetAndRaise(FilterTextProperty, ref _filterText, value);
    }

    public static readonly DirectProperty<VersionPickerDialog, string> SelectedTypeProperty =
        AvaloniaProperty.RegisterDirect<VersionPickerDialog, string>(nameof(SelectedType), o => o.SelectedType,
            (o, v) => o.SelectedType = v);

    private string _selectedType;

    public string SelectedType
    {
        get => _selectedType;
        set => SetAndRaise(SelectedTypeProperty, ref _selectedType, value);
    }

    public static readonly DirectProperty<VersionPickerDialog, string[]> TypesProperty =
        AvaloniaProperty.RegisterDirect<VersionPickerDialog, string[]>(nameof(Types), o => o.Types,
            (o, v) => o.Types = v);

    private string[] _types = [];

    public string[] Types
    {
        get => _types;
        set => SetAndRaise(TypesProperty, ref _types, value);
    }

    public static readonly DirectProperty<VersionPickerDialog, bool> IsLoadedProperty =
        AvaloniaProperty.RegisterDirect<VersionPickerDialog, bool>(nameof(IsLoaded), o => o.IsLoaded,
            (o, v) => o.IsLoaded = v);

    private bool _isLoaded;

    public bool IsLoaded
    {
        get => _isLoaded;
        set => SetAndRaise(IsLoadedProperty, ref _isLoaded, value);
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
            var items = (await _factory(_cts.Token)).ToList();
            Types = items.Select(x => x.TypeRaw).Distinct().ToArray();
            _subscription?.Dispose();
            _versions.AddOrUpdate(items);
            var filter = this.GetObservable(FilterTextProperty).Select(BuildFilterText);
            var type = this.GetObservable(SelectedTypeProperty).Select(BuildFilterType);
            _subscription = _versions.Connect().Filter(filter).Filter(type)
                .SortBy(x => x.ReleaseTimeRaw, SortDirection.Descending)
                .Bind(out var view).Subscribe();
            View = view;
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            // TODO: show error content
        }
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _cts.Cancel();
        _subscription?.Dispose();
    }

    private Func<GameVersionModel, bool> BuildFilterText(string filter) =>
        x => string.IsNullOrEmpty(filter) || x.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase);

    private Func<GameVersionModel, bool> BuildFilterType(string type) =>
        x => x.TypeRaw.Equals(type, StringComparison.InvariantCultureIgnoreCase);
}