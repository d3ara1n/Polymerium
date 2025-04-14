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

public partial class GameVersionPickerDialog : Dialog
{
    public static readonly DirectProperty<GameVersionPickerDialog, ReadOnlyObservableCollection<GameVersionModel>?>
        ViewProperty =
            AvaloniaProperty
               .RegisterDirect<GameVersionPickerDialog, ReadOnlyObservableCollection<GameVersionModel>?>(nameof(View),
                    o => o.View,
                    (o, v) => o.View = v);

    public static readonly DirectProperty<GameVersionPickerDialog, string> FilterTextProperty =
        AvaloniaProperty.RegisterDirect<GameVersionPickerDialog, string>(nameof(FilterText),
                                                                         o => o.FilterText,
                                                                         (o, v) => o.FilterText = v);

    public static readonly DirectProperty<GameVersionPickerDialog, string> SelectedTypeProperty =
        AvaloniaProperty.RegisterDirect<GameVersionPickerDialog, string>(nameof(SelectedType),
                                                                         o => o.SelectedType,
                                                                         (o, v) => o.SelectedType = v);

    public static readonly DirectProperty<GameVersionPickerDialog, string[]> TypesProperty =
        AvaloniaProperty.RegisterDirect<GameVersionPickerDialog, string[]>(nameof(Types),
                                                                           o => o.Types,
                                                                           (o, v) => o.Types = v);

    private readonly IDisposable _subscription;

    private readonly SourceCache<GameVersionModel, string> _versions = new(x => x.Name);

    public GameVersionPickerDialog()
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
        get;
        set => SetAndRaise(ViewProperty, ref field, value);
    }

    public string FilterText
    {
        get;
        set => SetAndRaise(FilterTextProperty, ref field, value);
    } = string.Empty;

    public string SelectedType
    {
        get;
        set => SetAndRaise(SelectedTypeProperty, ref field, value);
    } = string.Empty;

    public string[] Types
    {
        get;
        set => SetAndRaise(TypesProperty, ref field, value);
    } = [];

    public void SetItems(IReadOnlyList<GameVersionModel> versions)
    {
        Types = versions.Select(x => x.TypeRaw).Distinct().ToArray();
        _versions.Clear();
        _versions.AddOrUpdate(versions);
    }

    protected override bool ValidateResult(object? result) => result is GameVersionModel;

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _subscription.Dispose();
        base.OnUnloaded(e);
    }

    private Func<GameVersionModel, bool> BuildFilterText(string filter) =>
        x => string.IsNullOrEmpty(filter) || x.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase);

    private Func<GameVersionModel, bool> BuildFilterType(string type) =>
        x => x.TypeRaw.Equals(type, StringComparison.InvariantCultureIgnoreCase);
}