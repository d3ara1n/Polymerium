using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DynamicData;
using Polymerium.App.Models;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Components;

public partial class PackageContainer : UserControl
{
    public static readonly DirectProperty<PackageContainer, string> FilterTextProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, string>(nameof(FilterText),
                                                                  o => o.FilterText,
                                                                  (o, v) => o.FilterText = v);

    public static readonly DirectProperty<PackageContainer, ReadOnlyObservableCollection<InstancePackageModel>?>
        ViewProperty =
            AvaloniaProperty
               .RegisterDirect<PackageContainer, ReadOnlyObservableCollection<InstancePackageModel>?>(nameof(View),
                    o => o.View,
                    (o, v) => o.View = v);

    public static readonly DirectProperty<PackageContainer, SourceCache<InstancePackageModel, Profile.Rice.Entry>?>
        ItemsProperty =
            AvaloniaProperty
               .RegisterDirect<PackageContainer, SourceCache<InstancePackageModel, Profile.Rice.Entry>?>(nameof(Items),
                    o => o.Items,
                    (o, v) => o.Items = v);

    public static readonly DirectProperty<PackageContainer, int> TotalCountProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, int>(nameof(TotalCount),
                                                               o => o.TotalCount,
                                                               (o, v) => o.TotalCount = v);

    public static readonly DirectProperty<PackageContainer, ICommand?> PrimaryCommandProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, ICommand?>(nameof(PrimaryCommand),
                                                                     o => o.PrimaryCommand,
                                                                     (o, v) => o.PrimaryCommand = v);

    private IDisposable? _subscription;

    public PackageContainer() => InitializeComponent();

    public ICommand? PrimaryCommand
    {
        get;
        set => SetAndRaise(PrimaryCommandProperty, ref field, value);
    }

    public static readonly DirectProperty<PackageContainer, ICommand?> GotoExplorerCommandProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, ICommand?>(nameof(GotoExplorerCommand),
                                                                     o => o.GotoExplorerCommand,
                                                                     (o, v) => o.GotoExplorerCommand = v);

    public ICommand? GotoExplorerCommand
    {
        get;
        set => SetAndRaise(GotoExplorerCommandProperty, ref field, value);
    }


    public string FilterText
    {
        get;
        set => SetAndRaise(FilterTextProperty, ref field, value);
    } = string.Empty;

    public ReadOnlyObservableCollection<InstancePackageModel>? View
    {
        get;
        set => SetAndRaise(ViewProperty, ref field, value);
    }

    public SourceCache<InstancePackageModel, Profile.Rice.Entry>? Items
    {
        get;
        set
        {
            if (SetAndRaise(ItemsProperty, ref field, value))
            {
                _subscription?.Dispose();
                if (value is not null)
                {
                    var filter = this.GetObservable(FilterTextProperty).Select(BuildFilter);
                    _subscription = value.Connect().Filter(filter).Bind(out var view).Subscribe();
                    View = view;
                }
            }
        }
    }

    public int TotalCount
    {
        get;
        set => SetAndRaise(TotalCountProperty, ref field, value);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _subscription?.Dispose();
        base.OnUnloaded(e);
    }

    private static Func<InstancePackageModel, bool> BuildFilter(string filter) =>
        x => string.IsNullOrEmpty(filter)
          || (x is { ProjectName: { } name, Summary: { } summary }
           && (name.Contains(filter, StringComparison.InvariantCultureIgnoreCase)
            || summary.Contains(filter, StringComparison.InvariantCultureIgnoreCase)));
}