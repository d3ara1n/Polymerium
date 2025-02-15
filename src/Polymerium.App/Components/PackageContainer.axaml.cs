using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using DynamicData;
using Polymerium.App.Models;

namespace Polymerium.App.Components;

public partial class PackageContainer : UserControl
{
    public static readonly DirectProperty<PackageContainer, string> FilterTextProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, string>(nameof(FilterText), o => o.FilterText,
            (o, v) => o.FilterText = v);


    public static readonly DirectProperty<PackageContainer, ReadOnlyObservableCollection<InstancePackageModel>?>
        ViewProperty =
            AvaloniaProperty.RegisterDirect<PackageContainer, ReadOnlyObservableCollection<InstancePackageModel>?>(
                nameof(View), o => o.View, (o, v) => o.View = v);

    public static readonly DirectProperty<PackageContainer, SourceCache<InstancePackageModel, string>?> ItemsProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, SourceCache<InstancePackageModel, string>?>(nameof(Items),
            o => o.Items, (o, v) => o.Items = v);

    public static readonly DirectProperty<PackageContainer, int> TotalCountProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, int>(nameof(TotalCount), o => o.TotalCount,
            (o, v) => o.TotalCount = v);

    public static readonly DirectProperty<PackageContainer, ICommand?> OpenUrlCommandProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, ICommand?>(nameof(OpenUrlCommand), o => o.OpenUrlCommand,
            (o, v) => o.OpenUrlCommand = v);

    private ICommand? _openUrlCommand;

    public ICommand? OpenUrlCommand
    {
        get => _openUrlCommand;
        set => SetAndRaise(OpenUrlCommandProperty, ref _openUrlCommand, value);
    }


    private string _filterText = string.Empty;

    private SourceCache<InstancePackageModel, string>? _items;
    private IDisposable? _subscription;

    private int _totalCount;

    private ReadOnlyObservableCollection<InstancePackageModel>? _view;

    public PackageContainer()
    {
        InitializeComponent();
    }

    public string FilterText
    {
        get => _filterText;
        set => SetAndRaise(FilterTextProperty, ref _filterText, value);
    }

    public ReadOnlyObservableCollection<InstancePackageModel>? View
    {
        get => _view;
        set => SetAndRaise(ViewProperty, ref _view, value);
    }

    public SourceCache<InstancePackageModel, string>? Items
    {
        get => _items;
        set
        {
            if (SetAndRaise(ItemsProperty, ref _items, value))
            {
                _subscription?.Dispose();
                var filter = this.GetObservable(FilterTextProperty).Select(BuildFilter);
                if (value is not null)
                {
                    _subscription = value.Connect().Filter(filter).Bind(out var view).Subscribe();
                    View = view;
                }
            }
        }
    }

    public static readonly DirectProperty<PackageContainer, bool> IsLockedProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, bool>(nameof(IsLocked), o => o.IsLocked,
            (o, v) => o.IsLocked = v);

    private bool _isLocked;

    public bool IsLocked
    {
        get => _isLocked;
        set => SetAndRaise(IsLockedProperty, ref _isLocked, value);
    }

    public int TotalCount
    {
        get => _totalCount;
        set => SetAndRaise(TotalCountProperty, ref _totalCount, value);
    }

    private Func<InstancePackageModel, bool> BuildFilter(string filter)
    {
        return x => string.IsNullOrEmpty(filter) || (x is { Name : { } name, Summary: { } summary } &&
                                                     (name.Contains(filter,
                                                          StringComparison.InvariantCultureIgnoreCase) ||
                                                      summary.Contains(filter,
                                                          StringComparison.InvariantCultureIgnoreCase)));
    }
}