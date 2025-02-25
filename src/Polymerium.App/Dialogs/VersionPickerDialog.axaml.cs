using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Interactivity;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs;

public partial class VersionPickerDialog : Dialog
{
    public static readonly DirectProperty<VersionPickerDialog, ReadOnlyObservableCollection<GameVersionModel>?>
        ViewProperty =
            AvaloniaProperty
               .RegisterDirect<VersionPickerDialog, ReadOnlyObservableCollection<GameVersionModel>?>(nameof(View),
                    o => o.View,
                    (o, v) => o.View = v);

    public static readonly DirectProperty<VersionPickerDialog, string> FilterTextProperty =
        AvaloniaProperty.RegisterDirect<VersionPickerDialog, string>(nameof(FilterText),
                                                                     o => o.FilterText,
                                                                     (o, v) => o.FilterText = v);

    public static readonly DirectProperty<VersionPickerDialog, string> SelectedTypeProperty =
        AvaloniaProperty.RegisterDirect<VersionPickerDialog, string>(nameof(SelectedType),
                                                                     o => o.SelectedType,
                                                                     (o, v) => o.SelectedType = v);

    public static readonly DirectProperty<VersionPickerDialog, string[]> TypesProperty =
        AvaloniaProperty.RegisterDirect<VersionPickerDialog, string[]>(nameof(Types),
                                                                       o => o.Types,
                                                                       (o, v) => o.Types = v);

    private readonly IDisposable _subscription;


    private readonly SourceCache<GameVersionModel, string> _versions = new(x => x.Name);

    private string _filterText = string.Empty;

    private string _selectedType = string.Empty;

    private string[] _types = [];

    private ReadOnlyObservableCollection<GameVersionModel>? _view;


    public VersionPickerDialog()
    {
        InitializeComponent();

        _subscription?.Dispose();
        var filter = this.GetObservable(FilterTextProperty).Select(BuildFilterText);
        var type = this.GetObservable(SelectedTypeProperty).Select(BuildFilterType);
        _subscription = _versions
                       .Connect()
                       .Filter(filter)
                       .Filter(type)
                       .SortBy(x => x.ReleaseTimeRaw, SortDirection.Descending)
                       .Bind(out var view)
                       .Subscribe();
        View = view;
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

    public void SetItems(IReadOnlyList<GameVersionModel> versions)
    {
        Types = versions.Select(x => x.TypeRaw).Distinct().ToArray();
        _versions.Clear();
        _versions.AddOrUpdate(versions);
    }

    protected override bool ValidateResult(object? result) => result is GameVersionModel;

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _subscription.Dispose();
    }

    private Func<GameVersionModel, bool> BuildFilterText(string filter) =>
        x => string.IsNullOrEmpty(filter) || x.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase);

    private Func<GameVersionModel, bool> BuildFilterType(string type) =>
        x => x.TypeRaw.Equals(type, StringComparison.InvariantCultureIgnoreCase);
}