using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs;

public partial class VersionPickerDialog : Dialog
{
    public static readonly DirectProperty<VersionPickerDialog, ReadOnlyObservableCollection<GameVersionModel>?> ViewProperty = AvaloniaProperty.RegisterDirect<VersionPickerDialog, ReadOnlyObservableCollection<GameVersionModel>?>(nameof(View), o => o.View, (o, v) => o.View = v);

    public static readonly DirectProperty<VersionPickerDialog, string> FilterTextProperty = AvaloniaProperty.RegisterDirect<VersionPickerDialog, string>(nameof(FilterText), o => o.FilterText, (o, v) => o.FilterText = v);

    public static readonly DirectProperty<VersionPickerDialog, string> SelectedTypeProperty = AvaloniaProperty.RegisterDirect<VersionPickerDialog, string>(nameof(SelectedType), o => o.SelectedType, (o, v) => o.SelectedType = v);

    public static readonly DirectProperty<VersionPickerDialog, string[]> TypesProperty = AvaloniaProperty.RegisterDirect<VersionPickerDialog, string[]>(nameof(Types), o => o.Types, (o, v) => o.Types = v);

    public static readonly DirectProperty<VersionPickerDialog, bool> IsLoadedProperty = AvaloniaProperty.RegisterDirect<VersionPickerDialog, bool>(nameof(IsVersionLoaded), o => o.IsVersionLoaded, (o, v) => o.IsVersionLoaded = v);

    private readonly CancellationTokenSource _cts = new();
    private readonly Func<CancellationToken, Task<IReadOnlyList<GameVersionModel>>> _factory;
    private readonly SourceCache<GameVersionModel, string> _versions = new(x => x.Name);

    private string _filterText = string.Empty;

    private bool _isVersionVersionLoaded;

    private string _selectedType = string.Empty;
    private IDisposable? _subscription;

    private string[] _types = [];

    private ReadOnlyObservableCollection<GameVersionModel>? _view;


    public VersionPickerDialog(Func<CancellationToken, Task<IReadOnlyList<GameVersionModel>>> factory)
    {
        _factory = factory;
        InitializeComponent();
    }

    public ReadOnlyObservableCollection<GameVersionModel>? View
    {
        get => _view;
        set => SetAndRaise(ViewProperty, ref _view, value);
    }

    public string FilterText
    {
        get => _filterText;
        set => SetAndRaise(FilterTextProperty, ref _filterText, value);
    }

    public string SelectedType
    {
        get => _selectedType;
        set => SetAndRaise(SelectedTypeProperty, ref _selectedType, value);
    }

    public string[] Types
    {
        get => _types;
        set => SetAndRaise(TypesProperty, ref _types, value);
    }

    public bool IsVersionLoaded
    {
        get => _isVersionVersionLoaded;
        set => SetAndRaise(IsLoadedProperty, ref _isVersionVersionLoaded, value);
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
            _subscription = _versions.Connect().Filter(filter).Filter(type).SortBy(x => x.ReleaseTimeRaw, SortDirection.Descending).Bind(out var view).Subscribe();
            View = view;
            IsVersionLoaded = true;
        }
        catch (Exception)
        {
            // TODO: show error content
        }
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _cts.Cancel();
        _subscription?.Dispose();
    }

    private Func<GameVersionModel, bool> BuildFilterText(string filter) => x => string.IsNullOrEmpty(filter) || x.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase);

    private Func<GameVersionModel, bool> BuildFilterType(string type) => x => x.TypeRaw.Equals(type, StringComparison.InvariantCultureIgnoreCase);
}