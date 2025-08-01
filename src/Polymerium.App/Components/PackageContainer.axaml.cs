using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DynamicData;
using DynamicData.Binding;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Trident.Abstractions.Extensions;
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

    public static readonly DirectProperty<PackageContainer, ICommand?> UpdateBatchCommandProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, ICommand?>(nameof(UpdateBatchCommand),
                                                                     o => o.UpdateBatchCommand,
                                                                     (o, v) => o.UpdateBatchCommand = v);

    public static readonly
        DirectProperty<PackageContainer, ReadOnlyObservableCollection<InstancePackageFilterTagModel>?>
        TagsViewProperty =
            AvaloniaProperty
               .RegisterDirect<PackageContainer, ReadOnlyObservableCollection<InstancePackageFilterTagModel>
                    ?>(nameof(TagsView), o => o.TagsView, (o, v) => o.TagsView = v);

    public static readonly DirectProperty<PackageContainer, bool> IsFilterActiveProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, bool>(nameof(IsFilterActive),
                                                                o => o.IsFilterActive,
                                                                (o, v) => o.IsFilterActive = v);

    public static readonly DirectProperty<PackageContainer, int> LayoutIndexProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, int>(nameof(LayoutIndex),
                                                               o => o.LayoutIndex,
                                                               (o, v) => o.LayoutIndex = v);


    private CompositeDisposable _subscriptions = new();
    public PackageContainer() => InitializeComponent();

    public int LayoutIndex
    {
        get;
        set => SetAndRaise(LayoutIndexProperty, ref field, value);
    }

    public bool IsFilterActive
    {
        get;
        set => SetAndRaise(IsFilterActiveProperty, ref field, value);
    }

    public ReadOnlyObservableCollection<InstancePackageFilterTagModel>? TagsView
    {
        get;
        set => SetAndRaise(TagsViewProperty, ref field, value);
    }

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

    public ICommand? UpdateBatchCommand
    {
        get;
        set => SetAndRaise(UpdateBatchCommandProperty, ref field, value);
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
                _subscriptions.Dispose();
                _subscriptions.Clear();
                _subscriptions = new CompositeDisposable();
                if (value is not null)
                {
                    value
                       .Connect()
                       .MergeManyChangeSets(x => x.Tags.ToObservableChangeSet())
                       .Distinct()
                       .Transform(x => new InstancePackageFilterTagModel(x) { RefCount = 1 })
                       .Bind(out var tagsView)
                       .AutoRefresh()
                       .Filter(x => x.IsSelected)
                       .Transform(x => x.Content)
                       .Bind(out var filterTags)
                       .Subscribe()
                       .DisposeWith(_subscriptions);
                    TagsView = tagsView;

                    var text = this.GetObservable(FilterTextProperty).Select(BuildTextFilter);
                    var enability = this.GetObservable(FilterEnabilityProperty).Select(BuildEnabilityFilter);
                    var lockility = this.GetObservable(FilterLockilityProperty).Select(BuildLockilityFilter);
                    var kind = this.GetObservable(FilterKindProperty).Select(BuildKindFilter);
                    var tags = filterTags.ToObservableChangeSet().Select(_ => BuildTagFilter(filterTags));
                    value
                       .Connect()
                       .Filter(enability)
                       .Filter(lockility)
                       .Filter(kind)
                       .Filter(tags)
                       .Filter(text)
                       .Bind(out var view)
                       .Subscribe()
                       .DisposeWith(_subscriptions);
                    View = view;

                    filterTags
                       .ToObservableChangeSet()
                       .Select(_ => filterTags.Any())
                       .CombineLatest(this
                                     .GetObservable(FilterEnabilityProperty)
                                     .Select(x => x is { Enability: not null }),
                                      (x, y) => x || y)
                       .CombineLatest(this
                                     .GetObservable(FilterLockilityProperty)
                                     .Select(x => x is { Lockility: not null }),
                                      (x, y) => x || y)
                       .CombineLatest(this.GetObservable(FilterKindProperty).Select(x => x is { Kind: not null }),
                                      (x, y) => x || y)
                       .Subscribe(x => IsFilterActive = x)
                       .DisposeWith(_subscriptions);
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
        _subscriptions?.Dispose();
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
          || (x is
              {
                  ProjectId: { } pid,
                  ProjectName: { } name,
                  Author: { } author,
                  Summary: { } summary,
                  Version: { } version
              }
           && filter
             .Split(' ')
             .All(y => y switch
              {
                  ['@', .. var a] => author.Contains(a, StringComparison.OrdinalIgnoreCase),
                  ['#', .. var s] => summary.Contains(s, StringComparison.OrdinalIgnoreCase),
                  ['!', .. var i] => pid.Contains(i, StringComparison.OrdinalIgnoreCase)
                                  || (version is InstancePackageVersionModel v
                                   && v.Id.Contains(i, StringComparison.OrdinalIgnoreCase)),
                  _ => name.Contains(y, StringComparison.OrdinalIgnoreCase)
              }));

    private static Func<InstancePackageModel, bool> BuildTagFilter(ReadOnlyObservableCollection<string>? tags) =>
        x => tags is null or { Count: 0 } || tags.All(x.Tags.Contains);
}