using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DynamicData;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Components;

public partial class PackageContainer : UserControl
{
    public static readonly DirectProperty<PackageContainer, string> FilterTextProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, string>(nameof(FilterText),
                                                                  o => o.FilterText,
                                                                  (o, v) => o.FilterText = v);

    public static readonly DirectProperty<PackageContainer, PackageEntryEnabilityFilter?> FilterEnabilityProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, PackageEntryEnabilityFilter?>(nameof(FilterEnability),
            o => o.FilterEnability,
            (o, v) => o.FilterEnability = v);

    public static readonly DirectProperty<PackageContainer, PackageEntryLockilityFilter?> FilterLockilityProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, PackageEntryLockilityFilter?>(nameof(FilterLockility),
            o => o.FilterLockility,
            (o, v) => o.FilterLockility = v);


    public static readonly DirectProperty<PackageContainer, PackageEntryKindFilter?> FilterKindProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, PackageEntryKindFilter?>(nameof(FilterKind),
            o => o.FilterKind,
            (o, v) => o.FilterKind = v);


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

    public static readonly DirectProperty<PackageContainer, ICommand?> GotoExplorerCommandProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, ICommand?>(nameof(GotoExplorerCommand),
                                                                     o => o.GotoExplorerCommand,
                                                                     (o, v) => o.GotoExplorerCommand = v);

    public static readonly DirectProperty<PackageContainer, ICommand?> RemoveCommandProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, ICommand?>(nameof(RemoveCommand),
                                                                     o => o.RemoveCommand,
                                                                     (o, v) => o.RemoveCommand = v);

    public static readonly DirectProperty<PackageContainer, ICommand?> ExportListCommandProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, ICommand?>(nameof(ExportListCommand),
                                                                     o => o.ExportListCommand,
                                                                     (o, v) => o.ExportListCommand = v);

    public static readonly DirectProperty<PackageContainer, ICommand?> BulkUpdateCommandProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, ICommand?>(nameof(BulkUpdateCommand),
                                                                     o => o.BulkUpdateCommand,
                                                                     (o, v) => o.BulkUpdateCommand = v);


    private IDisposable? _subscription;
    public PackageContainer() => InitializeComponent();

    public PackageEntryEnabilityFilter? FilterEnability
    {
        get;
        set => SetAndRaise(FilterEnabilityProperty, ref field, value);
    }

    public PackageEntryLockilityFilter? FilterLockility
    {
        get;
        set => SetAndRaise(FilterLockilityProperty, ref field, value);
    }

    public PackageEntryKindFilter? FilterKind
    {
        get;
        set => SetAndRaise(FilterKindProperty, ref field, value);
    }

    public ICommand? BulkUpdateCommand
    {
        get;
        set => SetAndRaise(BulkUpdateCommandProperty, ref field, value);
    }


    public ICommand? ExportListCommand
    {
        get;
        set => SetAndRaise(ExportListCommandProperty, ref field, value);
    }

    public ICommand? RemoveCommand
    {
        get;
        set => SetAndRaise(RemoveCommandProperty, ref field, value);
    }

    public ICommand? PrimaryCommand
    {
        get;
        set => SetAndRaise(PrimaryCommandProperty, ref field, value);
    }

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
                    var text = this.GetObservable(FilterTextProperty).Select(BuildTextFilter);
                    var enability = this.GetObservable(FilterEnabilityProperty).Select(BuildEnabilityFilter);
                    var lockility = this.GetObservable(FilterLockilityProperty).Select(BuildLockilityFilter);
                    var kind = this.GetObservable(FilterKindProperty).Select(BuildKindFilter);
                    _subscription = value
                                   .Connect()
                                   .Filter(enability)
                                   .Filter(lockility)
                                   .Filter(kind)
                                   .Filter(text)
                                   .Bind(out var view)
                                   .Subscribe();
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

    private static Func<InstancePackageModel, bool> BuildEnabilityFilter(PackageEntryEnabilityFilter? enablity) =>
        x => enablity?.Enability is null || x.IsEnabled == enablity.Enability;

    private static Func<InstancePackageModel, bool> BuildLockilityFilter(PackageEntryLockilityFilter? lockility) =>
        x => lockility?.Lockility is null || x.IsLocked == lockility.Lockility;

    private static Func<InstancePackageModel, bool> BuildKindFilter(PackageEntryKindFilter? kind) =>
        x => kind?.Kind is null || x.Kind == kind.Kind;

    private static Func<InstancePackageModel, bool> BuildTextFilter(string filter) =>
        x => string.IsNullOrEmpty(filter)
          || (x is { ProjectName: { } name, Summary: { } summary }
           && (name.Contains(filter, StringComparison.InvariantCultureIgnoreCase)
            || summary.Contains(filter, StringComparison.InvariantCultureIgnoreCase)));
}