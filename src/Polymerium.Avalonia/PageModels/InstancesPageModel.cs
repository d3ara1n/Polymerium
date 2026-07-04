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
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Properties;
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
    OverlayService overlayService) : ViewModelBase
{
    private readonly CompositeDisposable _disposables = new();
    private readonly SourceCache<InstanceCardModel, string> _cards = new(x => x.Basic.Key);
    private readonly List<InstanceFilterBase> _filters = [];
    private IDisposable? _pipeline;

    #region Reactive

    [ObservableProperty]
    public partial string? FilterText { get; set; }

    [ObservableProperty]
    public partial int SortIndex { get; set; }

    [ObservableProperty]
    public partial bool AnyFilterActive { get; set; }

    public IReadOnlyList<InstanceFilterBase> Filters => _filters;

    public ReadOnlyObservableCollection<InstanceCardModel> View
    {
        get;
        private set => SetProperty(ref field, value);
    } = new(new());

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

        instanceService.PinnedChangeStream
                       .Subscribe(OnPinnedChanged)
                       .DisposeWith(_disposables);

        SetupFilters();

        Observable.CombineLatest(_filters.Select(f => f.WhenValueChanged(x => x.IsActive)).ToArray())
                  .Select(xs => xs.Any(x => x))
                  .Subscribe(x => AnyFilterActive = x)
                  .DisposeWith(_disposables);

        RebuildPipeline();

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

    private void SetupFilters()
    {
        _filters.Add(new MultiSelectInstanceFilter(
            _cards.Connect(),
            GetLoaderValues,
            Resources.InstancesPage_FilterLoaderLabel));

        _filters.Add(new MultiSelectInstanceFilter(
            _cards.Connect(),
            card => card.Tags,
            Resources.InstancesPage_FilterTagsLabel));
    }

    private static IEnumerable<string> GetLoaderValues(InstanceCardModel card)
    {
        yield return LoaderHelper.TryParse(card.Basic.Loader, out var result)
            ? LoaderHelper.ToDisplayName(result.Identity)
            : Resources.Enum_Vanilla;
    }

    private void RebuildPipeline()
    {
        _pipeline?.Dispose();
        var text = this.WhenValueChanged(x => x.FilterText).Select(BuildTextFilter);
        var predicates = _filters.Select(f => f.Predicate).Append(text).ToArray();
        var combined = Observable.CombineLatest(predicates)
                                .Select(xs => xs.Aggregate(
                                     new Func<InstanceCardModel, bool>(_ => true),
                                     (acc, p) => x => acc(x) && p(x)));

        _pipeline = _cards.Connect()
                          .Filter(combined)
                          .SortAndBind(out var view, BuildComparer(SortIndex))
                          .Subscribe();
        View = view;
    }

    private static Func<InstanceCardModel, bool> BuildTextFilter(string? filter) =>
        string.IsNullOrEmpty(filter)
            ? _ => true
            : x => x.Basic.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static IComparer<InstanceCardModel> BuildComparer(int sortIndex) => sortIndex switch
    {
        1 => SortExpressionComparer<InstanceCardModel>.Ascending(x => x.Basic.Name),
        _ => SortExpressionComparer<InstanceCardModel>.Descending(
            x => x.LastPlayedAtRaw ?? DateTimeOffset.MinValue
        ),
    };

    #endregion

    #region Profile events

    private InstanceCardModel BuildCard(string key, Profile item)
    {
        var model = new InstanceCardModel(
            key,
            item.Name,
            item.Setup.Version,
            item.Setup.Loader,
            item.Setup.Source
        )
        {
            IsPinned = instanceService.IsPinned(key),
            LastPlayedAtRaw =
                DateTimeHelper.FromPersistedLocalDateTime(persistenceService.GetLastActivity(key)?.End),
        };
        foreach (var tag in persistenceService.GetInstanceTags(key))
        {
            model.Tags.Add(tag);
        }

        return model;
    }

    private void OnProfileAdded(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => _cards.AddOrUpdate(BuildCard(e.Key, e.Value)));
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_cards.Lookup(e.Key) is { HasValue: true, Value: var existing })
            {
                existing.Basic.Name = e.Value.Name;
                existing.Basic.Version = e.Value.Setup.Version;
                existing.Basic.Loader = e.Value.Setup.Loader;
                existing.Basic.Source = e.Value.Setup.Source;
                existing.Basic.UpdateIcon();
            }
            else
            {
                _cards.AddOrUpdate(BuildCard(e.Key, e.Value));
            }
        });
    }

    private void OnProfileRemoved(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => _cards.RemoveKey(e.Key));
    }

    private void OnPinnedChanged(IChangeSet<string, string> change)
    {
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
    }

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
        var suggestions = _cards.Items
                                .SelectMany(c => c.Tags)
                                .Where(t => !card.Tags.Contains(t))
                                .Distinct()
                                .OrderBy(t => t)
                                .ToList();

        var dialog = new TagsEditorDialog
        {
            InitialTags = original,
            Suggestions = suggestions,
        };

        if (await overlayService.PopDialogAsync(dialog)
            && dialog.Result is IReadOnlyList<string> updated)
        {
            foreach (var removed in original.Except(updated).ToList())
            {
                card.Tags.Remove(removed);
            }
            foreach (var added in updated.Except(original).ToList())
            {
                card.Tags.Add(added);
            }

            persistenceService.SetInstanceTags(key, updated.ToArray());
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

    #endregion
}
