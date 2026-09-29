using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
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
    private const string VANILLA_LABEL_KEY = "Enum_Vanilla";
    private const string UNGROUPED_LABEL_KEY = "InstancesPage_GroupNone";
    private const string FILTER_LOADER_LABEL_KEY = "InstancesPage_FilterLoaderLabel";
    private const string FILTER_TAGS_LABEL_KEY = "InstancesPage_FilterTagsLabel";
    private const string GROUP_SNAPSHOT_LABEL_KEY = "InstancesPage_GroupSnapshot";
    private const string GROUP_OTHER_LABEL_KEY = "InstancesPage_GroupOther";
    private const string GROUP_NEVER_LABEL_KEY = "InstancesPage_GroupNever";
    private const string GROUP_TODAY_LABEL_KEY = "InstancesPage_GroupToday";
    private const string GROUP_THIS_WEEK_LABEL_KEY = "InstancesPage_GroupThisWeek";
    private const string GROUP_THIS_MONTH_LABEL_KEY = "InstancesPage_GroupThisMonth";
    private const string GROUP_EARLIER_LABEL_KEY = "InstancesPage_GroupEarlier";

    #region Reactive

    [ObservableProperty]
    public partial string? FilterText { get; set; }

    [ObservableProperty]
    public partial int OrderIndex { get; set; }

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

        GroupIndex = configurationService.Value.ApplicationInterfaceInstancesPageGroup;
        OrderIndex = configurationService.Value.ApplicationInterfaceInstancesPageOrder;

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

    partial void OnOrderIndexChanged(int value)
    {
        configurationService.Value.ApplicationInterfaceInstancesPageOrder = value;
        RebuildPipeline();
    }

    partial void OnGroupIndexChanged(int value)
    {
        configurationService.Value.ApplicationInterfaceInstancesPageGroup = value;
        RebuildPipeline();
    }

    private void SetupFilters()
    {
        _filters.Add(new MultiSelectInstanceFilter(_cards.Connect(),
                                                   GetLoaderValues,
                                                   FILTER_LOADER_LABEL_KEY));

        _filters.Add(new MultiSelectInstanceFilter(_cards.Connect(),
                                                   card => card.Tags,
                                                   FILTER_TAGS_LABEL_KEY));
    }

    private static IEnumerable<string> GetLoaderValues(InstanceCardModel card)
    {
        yield return GetLoaderValue(card);
    }

    private static string GetLoaderValue(InstanceCardModel card) =>
        LoaderHelper.TryParse(card.Basic.Loader, out var result)
            ? LoaderHelper.ToDisplayName(result.Identity)
            : VANILLA_LABEL_KEY;

    private void RebuildPipeline()
    {
        _pipeline?.Dispose();
        var text = this.WhenValueChanged(x => x.FilterText).Select(BuildTextFilter);
        var predicates = _filters.Select(f => f.Predicate).Append(text).ToArray();
        var combined = Observable
                      .CombineLatest(predicates)
                      .Select(xs => xs.Aggregate(new Func<InstanceCardModel, bool>(_ => true),
                                                 (acc, p) => x => acc(x) && p(x)));

        var comparer = BuildInstanceComparer(OrderIndex);
        var bound = _cards.Connect()
                          .Filter(combined)
                          .Group(GetGroupKey)
                          .Transform(g => new InstanceGroupModel(g, comparer))
                          .DisposeMany()
                          .AutoRefresh(x => x.FirstCardVersion)
                          .SortAndBind(out var groups, BuildGroupComparer(comparer));

        _pipeline = bound.Subscribe();
        Groups = groups;
    }

    private InstanceGroupKey GetGroupKey(InstanceCardModel card) => GroupIndex switch
    {
        1 => GetLoaderGroupKey(card),
        2 => GetVersionGroupKey(card.Basic.Version),
        3 => GetLastPlayedGroupKey(card.LastPlayedAtRaw),
        _ => new(
            "none",
            LocalizedLabelBase.Key(UNGROUPED_LABEL_KEY))
    };

    private static InstanceGroupKey GetLoaderGroupKey(InstanceCardModel card)
    {
        var value = GetLoaderValue(card);
        return new(
            $"loader:{value}",
            LocalizedLabelBase.Key(value));
    }

    private static InstanceGroupKey GetVersionGroupKey(string value)
    {
        var weeklySnapshot = LegacySnapshotRegex().Match(value.Trim());
        if (weeklySnapshot.Success)
        {
            var year = 2000 + int.Parse(weeklySnapshot.Groups["year"].Value);
            return new(
                $"snapshot:{year}",
                LocalizedLabelBase.Formatted(GROUP_SNAPSHOT_LABEL_KEY, year));
        }

        var dottedVersion = DottedVersionRegex().Match(value.Trim());
        if (!dottedVersion.Success)
        {
            return new(
                "version:other",
                LocalizedLabelBase.Key(GROUP_OTHER_LABEL_KEY));
        }

        var major = int.Parse(dottedVersion.Groups["major"].Value);
        var minor = int.Parse(dottedVersion.Groups["minor"].Value);
        var groupMinor = major == 1 ? minor : 0;
        var label = major == 1 ? $"1.{minor}" : major.ToString();
        return new(
            $"version:{major}:{groupMinor}",
            LocalizedLabelBase.Literal(label));
    }

    private static InstanceGroupKey GetLastPlayedGroupKey(DateTimeOffset? lastPlayed)
    {
        var label = lastPlayed is null
            ? GROUP_NEVER_LABEL_KEY
            : BucketLastPlayed(lastPlayed.Value);
        return new(
            $"last-played:{label}",
            LocalizedLabelBase.Key(label));
    }

    private static string BucketLastPlayed(DateTimeOffset lastPlayed)
    {
        var now = DateTimeOffset.Now;
        return lastPlayed.Date == now.Date
            ? GROUP_TODAY_LABEL_KEY
            : now - lastPlayed < TimeSpan.FromDays(7)
                ? GROUP_THIS_WEEK_LABEL_KEY
                : lastPlayed.Year == now.Year && lastPlayed.Month == now.Month
                    ? GROUP_THIS_MONTH_LABEL_KEY
                    : GROUP_EARLIER_LABEL_KEY;
    }

    private static IComparer<InstanceGroupModel> BuildGroupComparer(IComparer<InstanceCardModel> comparer) =>
        Comparer<InstanceGroupModel>.Create((a, b) =>
        {
            var result = (a.FirstCard, b.FirstCard) switch
            {
                (null, null) => 0,
                (null, _) => 1,
                (_, null) => -1,
                (var first, var second) => comparer.Compare(first, second)
            };

            return result != 0
                ? result
                : string.CompareOrdinal(a.Key.Identity, b.Key.Identity);
        });

    private static IComparer<InstanceCardModel> BuildInstanceComparer(int sortIndex) =>
        Comparer<InstanceCardModel>.Create((a, b) =>
        {
            var result = sortIndex switch
            {
                1 => CompareLastPlayed(a, b, ascending: true),
                2 => string.Compare(a.Basic.Name, b.Basic.Name, StringComparison.OrdinalIgnoreCase),
                3 => string.Compare(b.Basic.Name, a.Basic.Name, StringComparison.OrdinalIgnoreCase),
                _ => CompareLastPlayed(a, b, ascending: false)
            };

            return result != 0
                ? result
                : string.CompareOrdinal(a.Basic.Key, b.Basic.Key);
        });

    private static int CompareLastPlayed(
        InstanceCardModel a,
        InstanceCardModel b,
        bool ascending)
    {
        if (a.LastPlayedAtRaw is null || b.LastPlayedAtRaw is null)
        {
            if (a.LastPlayedAtRaw is null && b.LastPlayedAtRaw is null)
            {
                return 0;
            }

            return a.LastPlayedAtRaw is null ? 1 : -1;
        }

        var result = a.LastPlayedAtRaw.Value.CompareTo(b.LastPlayedAtRaw.Value);
        return ascending ? result : -result;
    }

    [GeneratedRegex(@"^(?<year>\d{2})w\d{2}[a-z]$", RegexOptions.IgnoreCase)]
    private static partial Regex LegacySnapshotRegex();

    [GeneratedRegex(@"^(?<major>\d+)\.(?<minor>\d+)(?:\.|-|$)")]
    private static partial Regex DottedVersionRegex();

    private static Func<InstanceCardModel, bool> BuildTextFilter(string? filter) =>
        string.IsNullOrEmpty(filter)
            ? _ => true
            : x => x.Basic.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);


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
