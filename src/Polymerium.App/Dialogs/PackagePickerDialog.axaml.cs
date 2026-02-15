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
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Dialogs;

public partial class PackagePickerDialog : Dialog
{
    public static readonly DirectProperty<PackagePickerDialog, ReadOnlyObservableCollection<InstancePackageModel>?>
        ViewProperty =
            AvaloniaProperty
               .RegisterDirect<PackagePickerDialog, ReadOnlyObservableCollection<InstancePackageModel>?>(nameof(View),
                    o => o.View,
                    (o, v) => o.View = v);

    public static readonly DirectProperty<PackagePickerDialog, string> FilterTextProperty =
        AvaloniaProperty.RegisterDirect<PackagePickerDialog, string>(nameof(FilterText),
                                                                     o => o.FilterText,
                                                                     (o, v) => o.FilterText = v);

    private readonly IDisposable _subscription;
    private readonly SourceCache<InstancePackageModel, string> _packages = new(x => x.Entry.Purl);

    public PackagePickerDialog()
    {
        InitializeComponent();

        _subscription?.Dispose();
        var filterText = this.GetObservable(FilterTextProperty).Select(BuildFilterText);
        _subscription = _packages
                       .Connect()
                       .Filter(filterText)
                       .SortBy(x => x.Info?.ProjectName ?? string.Empty)
                       .Bind(out var view)
                       .Subscribe();
        View = view;
    }

    public ReadOnlyObservableCollection<InstancePackageModel>? View
    {
        get;
        set => SetAndRaise(ViewProperty, ref field, value);
    }

    public string FilterText
    {
        get;
        set => SetAndRaise(FilterTextProperty, ref field, value);
    } = string.Empty;

    public void SetItems(IReadOnlyList<InstancePackageModel> packages)
    {
        _packages.Clear();
        _packages.AddOrUpdate(packages.Where(x => x.Info != null));
    }

    protected override bool ValidateResult(object? result) => result is InstancePackageModel;

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _subscription.Dispose();
        base.OnUnloaded(e);
    }

    private Func<InstancePackageModel, bool> BuildFilterText(string filter) =>
        x => string.IsNullOrEmpty(filter)
          || (x.Info?.ProjectName?.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ?? false)
          || (x.Info?.Label?.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ?? false)
          || x.Entry.Purl.Contains(filter, StringComparison.InvariantCultureIgnoreCase);

}
