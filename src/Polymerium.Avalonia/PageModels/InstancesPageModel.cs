using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;

namespace Polymerium.Avalonia.PageModels;

public partial class InstancesPageModel(
    ProfileManager profileManager,
    PersistenceService persistenceService,
    InstanceService instanceService,
    NavigationService navigationService,
    ConfigurationService configurationService,
    OverlayService overlayService) : ViewModelBase
{
    private readonly SourceCache<InstanceCardModel, string> _cards = new(x => x.Basic.Key);
    private readonly CompositeDisposable _disposables = new();
    private readonly List<InstanceFilterBase> _filters = [];
    private IDisposable? _pipeline;
    private const string VANILLA_LABEL = "Enum_Vanilla";
    private const string UNGROUPED_LABEL = "InstancesPage_GroupNone";

    #region Reactive

    [ObservableProperty]
    public partial string? FilterText { get; set; }

    [ObservableProperty]
    public partial int SortIndex { get; set; }

    [ObservableProperty]
    public partial int GroupIndex { get; set; }

    [ObservableProperty]
    public partial bool AnyFilterActive { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; private set; }

    public IReadOnlyList<InstanceFilterBase> Filters => _filters;

    public ReadOnlyObservableCollection<InstanceGroupModel> Groups
    {
        get;
        private set => SetProperty(ref field, value);
    } = [with([])];

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        foreach (var (key, item) in profileManager.Profiles)
        {
            _cards.AddOrUpdate(BuildCard(key, item));
        }

        profileManager.ProfileAdded += OnProfileAdded;
        profileManager.ProfileUpdated += OnProfileUpdated;
        profileManager.ProfileRemoved += OnProfileRemoved;

        instanceService.PinnedChangeStream.Subscribe(OnPinnedChanged).DisposeWith(_disposables);

        SetupFilters();

        Observable
           .CombineLatest([.. _filters.Select(f => f.WhenValueChanged(x => x.IsActive))])
           .Select(xs => xs.Any(x => x))
           .Subscribe(x => AnyFilterActive = x)
           .DisposeWith(_disposables);

        GroupIndex = configurationService.Value.ApplicationInterfaceInstancesPageGrouping;

        RebuildPipeline();

        _cards.CountChanged.Subscribe(x => TotalCount = x).DisposeWith(_disposables);

        return base.OnInitializeAsync(token);
    }

    protected override Task OnDeinitializeAsync()
    {
        _pipeline?.Dispose();
        foreach (var filter in _filters)
        {
            filter.Dispose();
        }

        _disposables.Dispose();

        profileManager.ProfileAdded -= OnProfileAdded;
        profileManager.ProfileUpdated -= OnProfileUpdated;
        profileManager.ProfileRemoved -= OnProfileRemoved;

        return base.OnDeinitializeAsync();
    }

    #endregion

    #region Pipeline

    partial void OnSortIndexChanged(int value) => RebuildPipeline();

    partial void OnGroupIndexChanged(int value)
    {
        configurationService.Value.ApplicationInterfaceInstancesPageGrouping = value;
        RebuildPipeline();
    }

    private void SetupFilters()
    {
        _filters.Add(new MultiSelectInstanceFilter(_cards.Connect(),
                                                   GetLoaderValues,
                                                   "InstancesPage_FilterLoaderLabel"));

        _filters.Add(new MultiSelectInstanceFilter(_cards.Connect(),
                                                   card => card.Tags,
                                                   "InstancesPage_FilterTagsLabel"));
    }

    private static IEnumerable<string> GetLoaderValues(InstanceCardModel card)
    {
        yield return GetLoaderValue(card);
    }

    private static string GetLoaderValue(InstanceCardModel card) =>
        LoaderHelper.TryParse(card.Basic.Loader, out var result)
            ? LoaderHelper.ToDisplayName(result.Identity)
            : VANILLA_LABEL;

    private void RebuildPipeline()
    {
        _pipeline?.Dispose();
        var text = this.WhenValueChanged(x => x.FilterText).Select(BuildTextFilter);
        var predicates = _filters.Select(f => f.Predicate).Append(text).ToArray();
        var combined = Observable
                      .CombineLatest(predicates)
                      .Select(xs => xs.Aggregate(new Func<InstanceCardModel, bool>(_ => true),
                                                 (acc, p) => x => acc(x) && p(x)));

        var comparer = BuildComparer(SortIndex);
        var bound = _cards.Connect()
                          .Filter(combined)
                          .Group(GetGroupKey)
                          .Transform(g => new InstanceGroupModel(g, comparer))
                          .DisposeMany()
                          .SortAndBind(out var groups, BuildGroupComparer(GroupIndex));

        _pipeline = bound.Subscribe();
        Groups = groups;
    }

    private string GetGroupKey(InstanceCardModel card) => GroupIndex switch
    {
        1 => GetLoaderValue(card),
        2 => card.Basic.Version,
        3 => BucketLastPlayed(card.LastPlayedAtRaw),
        _ => UNGROUPED_LABEL
    };

    private static string BucketLastPlayed(DateTimeOffset? lastPlayed)
    {
        if (lastPlayed is null)
        {
            return "InstancesPage_GroupNever";
        }

        var now = DateTimeOffset.Now;
        return lastPlayed.Value.Date == now.Date
            ? "InstancesPage_GroupToday"
            : now - lastPlayed.Value < TimeSpan.FromDays(7)
                ? "InstancesPage_GroupThisWeek"
                : lastPlayed.Value.Year == now.Year && lastPlayed.Value.Month == now.Month
                    ? "InstancesPage_GroupThisMonth"
                    : "InstancesPage_GroupEarlier";
    }

    private static IComparer<InstanceGroupModel> BuildGroupComparer(int groupIndex) => groupIndex switch
    {
        1 => Comparer<InstanceGroupModel>.Create(static (a, b) =>
        {
            if (a.Label == VANILLA_LABEL)
            {
                return b.Label == VANILLA_LABEL ? 0 : -1;
            }

            return b.Label == VANILLA_LABEL ? 1 : string.CompareOrdinal(a.Label, b.Label);
        }),
        2 => Comparer<InstanceGroupModel>.Create(static (a, b) =>
            Version.TryParse(a.Label, out var va) && Version.TryParse(b.Label, out var vb)
                ? vb.CompareTo(va)
                : string.CompareOrdinal(b.Label, a.Label)),
        3 => Comparer<InstanceGroupModel>.Create(static (a, b) => RankOfBucket(a.Label).CompareTo(RankOfBucket(b.Label))),
        _ => Comparer<InstanceGroupModel>.Default
    };

    private static int RankOfBucket(string label) => label switch
    {
        "InstancesPage_GroupToday" => 0,
        "InstancesPage_GroupThisWeek" => 1,
        "InstancesPage_GroupThisMonth" => 2,
        "InstancesPage_GroupEarlier" => 3,
        _ => 4
    };

    private static Func<InstanceCardModel, bool> BuildTextFilter(string? filter) =>
        string.IsNullOrEmpty(filter)
            ? _ => true
            : x => x.Basic.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static IComparer<InstanceCardModel> BuildComparer(int sortIndex) =>
        sortIndex switch
        {
            1 => SortExpressionComparer<InstanceCardModel>.Ascending(x => x.Basic.Name),
            _ => SortExpressionComparer<InstanceCardModel>.Descending(x => x.LastPlayedAtRaw ?? DateTimeOffset.MinValue)
        };

    #endregion

    #region Profile events

    private InstanceCardModel BuildCard(string key, Profile item)
    {
        var model = new InstanceCardModel(key, item.Name, item.Setup.Version, item.Setup.Loader, item.Setup.Source)
        {
            IsPinned = instanceService.IsPinned(key),
            LastPlayedAtRaw =
                DateTimeHelper.FromPersistedLocalDateTime(persistenceService.GetLastActivity(key)?.End)
        };
        foreach (var tag in persistenceService.GetInstanceTags(key))
        {
            model.Tags.Add(tag);
        }

        return model;
    }

    private void OnProfileAdded(object? sender, ProfileManager.ProfileChangedEventArgs e) =>
        Dispatcher.UIThread.Post(() => _cards.AddOrUpdate(BuildCard(e.Key, e.Value)));

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (_cards.Lookup(e.Key) is { HasValue: true, Value: var existing })
            {
                existing.Basic.Name = e.Value.Name;
                existing.Basic.Version = e.Value.Setup.Version;
                existing.Basic.Loader = e.Value.Setup.Loader;
                existing.Basic.Source = e.Value.Setup.Source;
                existing.Basic.UpdateIcon();
                _cards.Refresh(existing);
            }
            else
            {
                _cards.AddOrUpdate(BuildCard(e.Key, e.Value));
            }
        });

    private void OnProfileRemoved(object? sender, ProfileManager.ProfileChangedEventArgs e) =>
        Dispatcher.UIThread.Post(() => _cards.RemoveKey(e.Key));

    private void OnPinnedChanged(IChangeSet<string, string> change) =>
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var item in change)
            {
                if (_cards.Lookup(item.Key) is { HasValue: true, Value: var card })
                {
                    card.IsPinned = item.Reason is ChangeReason.Add or ChangeReason.Update;
                }
            }
        });

    #endregion

    #region Commands

    [RelayCommand]
    private void ViewInstance(string? key)
    {
        if (key is not null)
        {
            navigationService.Navigate<InstancePage>(key);
        }
    }

    [RelayCommand]
    private void NewInstance() => navigationService.Navigate<NewInstancePage>();

    [RelayCommand]
    private void GotoMarketplace() => navigationService.Navigate<MarketplaceModpacksPage>();

    [RelayCommand]
    private void Migrate() => overlayService.PopModal<MigrateModal>();

    [RelayCommand]
    private void Play(string key) => instanceService.Play(key);

    [RelayCommand]
    private void Deploy(string key) => instanceService.Deploy(key);

    [RelayCommand]
    private Task ExportInstance(string? key) => instanceService.ExportInstanceAsync(key);

    [RelayCommand]
    private Task OpenFolder(string? key) => instanceService.OpenFolder(key);

    [RelayCommand]
    private void GotoSetup(string? key) => instanceService.GotoSetup(key);

    [RelayCommand]
    private void GotoProperties(string? key) => instanceService.GotoProperties(key);

    [RelayCommand]
    private void Pin(string? key)
    {
        if (key != null)
        {
            instanceService.Pin(key);
        }
    }

    [RelayCommand]
    private void Unpin(string? key)
    {
        if (key != null)
        {
            instanceService.Unpin(key);
        }
    }

    [RelayCommand]
    private async Task EditTags(string? key)
    {
        if (key is null || _cards.Lookup(key) is not { HasValue: true, Value: var card })
        {
            return;
        }

        var original = card.Tags.ToArray();
        var suggestions = _cards
                         .Items.SelectMany(c => c.Tags)
                         .Where(t => !card.Tags.Contains(t))
                         .Distinct()
                         .OrderBy(t => t)
                         .ToList();

        var dialog = new TagsEditorDialog { InitialTags = original, Suggestions = suggestions };

        if (await overlayService.PopDialogAsync(dialog) && dialog.Result is IReadOnlyList<string> updated)
        {
            foreach (var removed in original.Except(updated).ToList())
            {
                card.Tags.Remove(removed);
            }

            foreach (var added in updated.Except(original).ToList())
            {
                card.Tags.Add(added);
            }

            persistenceService.SetInstanceTags(key, [.. updated]);
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        foreach (var filter in _filters)
        {
            filter.Clear();
        }
    }

    [RelayCommand]
    private void ToggleGroup(InstanceGroupModel? group)
    {
        if (group is not null)
        {
            group.IsExpanded = !group.IsExpanded;
        }
    }

    #endregion
}
