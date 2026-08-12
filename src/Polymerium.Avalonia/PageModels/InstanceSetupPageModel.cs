using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CsvHelper;
using CsvHelper.Configuration;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Toasts;
using Polymerium.Avalonia.Utilities;
using Refit;
using TridentCore.Abstractions.Extensions;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Engines.Deploying;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using TridentCore.Pref;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

namespace Polymerium.Avalonia.PageModels;

public partial class InstanceSetupPageModel(
    IViewContext<InstancePageModelBase.InstanceContextParameter> context,
    ILogger<InstanceSetupPageModel> logger,
    IServiceProvider serviceProvider,
    ProfileManager profileManager,
    NotificationService notificationService,
    InstanceStateAggregator aggregator,
    InstanceManager instanceManager,
    PackageMaterializer packageMaterializer,
    DataService dataService,
    OverlayService overlayService,
    NavigationService navigationService,
    PersistenceService persistenceService)
    : InstancePageModelBase(context, aggregator, instanceManager, profileManager),
      IStatefulViewModel<InstanceSetupPageModel.StateView>,
      IViewStateKeyProvider
{
    #region Nested type: ExportedEntry

    private record ExportedEntry(
        string Pref,
        string? Label,
        string? Namespace,
        string? ProjectId,
        string? VersionId,
        bool Enabled,
        string? Source,
        string? Name,
        string? Version,
        string Tags);

    #endregion

    #region Nested type: RefreshIntermediateData

    private class RefreshIntermediateData(InstancePackageModel model)
    {
        public InstancePackageModel Model => model;
        public Bitmap? Thumbnail { get; set; }
        public Project? Project { get; set; }
        public Package? Package { get; set; }
    }

    #endregion

    #region Nested type: StateData

    public partial class StateView : ModelBase
    {
        [ObservableProperty]
        public partial int LayoutIndex { get; set; }

        #region For PackageBulkUpdatePreviewerDialog

        public bool LastChosenIsEnabledOnly { get; set; } = true;
        public IReadOnlyList<string>? LastChosenTags { get; set; }
        public PackageBulkUpdatePreviewerTagPolicy LastChosenTagPolicy { get; set; }

        #endregion
    }

    #endregion

    #region Other

    private void TriggerPackageMerge()
    {
        var token = _pageCancellationTokenSource?.Token;
        if (token is null || token.Value.IsCancellationRequested)
        {
            return;
        }

        if (ProfileManager.TryGetImmutable(Basic.Key, out var profile))
        {
            // NOTE: Entry 按地址比较，仍存在的包不动其 Entry 项（实例稳定）；信息是否陈旧由
            //  RefreshMetadataAsync 现场重判，这里不预判。
            var lookup = profile.Setup.Packages.ToHashSet();
            var toRemove = new List<PackageListKey>();
            var entryCount = 0;
            foreach (var item in _flat.Items.OfType<PackageListItemBase.Entry>())
            {
                entryCount++;
                if (!lookup.Remove(item.Package.Entry))
                {
                    toRemove.Add(item.Key);
                }
            }

            _flat.Remove(toRemove);
            var persistentIndex = entryCount - toRemove.Count;
            var toAdd = lookup
                       .Select(x =>
                        {
                            var pkg = new InstancePackageModel(x, PackageSourceHelper.CanUpdate(x.Source, Basic.Source))
                            {
                                PersistentIndex = persistentIndex++
                            };
                            return new PackageListItemBase.Entry
                            {
                                Key = new PackageListKey.Entry(x),
                                Group = GroupModelOf(pkg),
                                Package = pkg
                            };
                        })
                       .ToList();
            _flat.AddOrUpdate(toAdd);

            // NOTE: 组头同步的不变式：每个有成员的非散装组恰好一个 Header，空组无 Header。
            var presentGroups = _flat
                               .Items.OfType<PackageListItemBase.Entry>()
                               .Select(i => i.Group)
                               .Where(g => g is not LooseGroupModel)
                               .Distinct()
                               .ToList();

            var bySource = presentGroups.ToDictionary(g => g.Source!, g => g);
            var desiredKeys = bySource.Keys.Select(s => new PackageListKey.Header(s)).ToHashSet();
            var currentKeys = _flat.Keys.OfType<PackageListKey.Header>().ToHashSet();
            _flat.Remove([.. currentKeys.Except(desiredKeys)]);
            foreach (var key in desiredKeys.Except(currentKeys))
            {
                _flat.AddOrUpdate(new PackageListItemBase.Header { Key = key, Group = bySource[key.Source] });
            }

            _metadataTask = RefreshMetadataAsync(_metadataTask, token.Value);
        }
    }

    private void TriggerReferenceRefresh()
    {
        var token = _pageCancellationTokenSource?.Token;
        if (token is null || token.Value.IsCancellationRequested)
        {
            return;
        }

        // NOTE: Basic 由 InstancePageModel 维护，理论上 ProfileUpdated 会先更新，但不可靠。
        if (ProfileManager.TryGetImmutable(Basic.Key, out var profile))
        {
            if (profile.Setup.Source is not null)
            {
                if (Reference is null
                 || (Reference is { Value: InstanceReferenceModel { } reference }
                  && reference.Pref != profile.Setup.Source))
                {
                    if (PackageHelper.TryParse(profile.Setup.Source, out var r))
                    {
                        Reference = new(async _ =>
                        {
                            var package = await dataService.ResolvePackageAsync(r,
                                                                                    Filter.None with
                                                                                    {
                                                                                        Kind = ResourceKind.Modpack
                                                                                    });

                            return new InstanceReferenceModel(profile.Setup.Source,
                                                              r.Repository,
                                                              package.ProjectName,
                                                              package.VersionId,
                                                              package.VersionName,
                                                              package.Thumbnail,
                                                              package.Reference);
                        });
                    }
                }
            }
            else
            {
                foreach (var model in _flat.Items.OfType<PackageListItemBase.Entry>().Select(i => i.Package))
                {
                    model.CanUpdate = true;
                }
            }
        }
    }

    private async Task RefreshMetadataAsync(Task previous, CancellationToken token)
    {
        // NOTE: 吞掉上一个任务的异常，避免 faulted 任务卡死整条队列。
        try
        {
            await previous;
        }
        catch { }

        token.ThrowIfCancellationRequested();

        // NOTE: 排到时若前一个已完成全部加载，这里重判为空，直接 no-op 完成。
        var pendingPackages = _flat
                             .Items.OfType<PackageListItemBase.Entry>()
                             .Select(i => i.Package)
                             .Where(p => p.Info is null || p.OldPrefCache != p.Entry.Pref)
                             .ToList();
        var pendingGroups = _flat
                           .Items.OfType<PackageListItemBase.Entry>()
                           .Select(i => i.Group)
                           .Where(g => g is not LooseGroupModel)
                           .Distinct()
                           .Where(g => g.Info is null)
                           .ToList();
        if (pendingPackages.Count == 0 && pendingGroups.Count == 0)
        {
            return;
        }

        IsRefreshing = true;
        try
        {
            await Task.WhenAll(RefreshPackageInfoAsync(pendingPackages, token),
                               RefreshGroupInfoAsync(pendingGroups, token));
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task RefreshPackageInfoAsync(IReadOnlyList<InstancePackageModel> packages, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();

            foreach (var package in packages)
            {
                package.IsLoaded = false;
            }

            var items = packages
                       .Select(x => PackageHelper.TryParse(x.Entry.Pref, out var pref)
                                        ? (Model: x, Pref: pref, Data: new RefreshIntermediateData(x))
                                        : throw new FormatException($"Failed to parse pref: {x.Entry.Pref}"))
                       .ToList();

            foreach (var sourceGroup in items.GroupBy(x => x.Model.Entry.Source))
            {
                token.ThrowIfCancellationRequested();

                var known = sourceGroup.Where(x => x.Pref.Version is not null).ToArray();
                if (known.Length > 0)
                {
                    var resolved =
                        await dataService.ResolvePackagesAsync(known.Select(x => x.Pref).Distinct(), Filter.None);
                    foreach (var (id, package) in resolved.Successful)
                    {
                        foreach (var item in known.Where(x => x.Pref == id))
                        {
                            item.Data.Package = package;
                        }
                    }
                }

                var unknown = sourceGroup.Where(x => x.Pref.Version is null).ToArray();
                if (unknown.Length > 0)
                {
                    var queried =
                        await dataService.QueryProjectsAsync(unknown
                                                            .Select(x => x.Pref.ToProjectIdentifier())
                                                            .Distinct());
                    foreach (var (projectKey, project) in queried.Successful)
                    {
                        var id = projectKey.ToPackageIdentifier();
                        foreach (var item in unknown.Where(x => x.Pref == id))
                        {
                            item.Data.Project = project;
                        }
                    }
                }
            }

            await Task.WhenAll(items.Select(async item =>
            {
                var thumbnail = item.Data.Package?.Thumbnail ?? item.Data.Project?.Thumbnail;
                if (thumbnail is null)
                {
                    item.Data.Thumbnail = AssetUriIndex.DirtImageBitmap;
                    return;
                }

                try
                {
                    item.Data.Thumbnail = await dataService.GetBitmapAsync(thumbnail);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    item.Data.Thumbnail = AssetUriIndex.DirtImageBitmap;
                }
            }));

            token.ThrowIfCancellationRequested();

            foreach (var item in items)
            {
                var x = item.Data;
                InstancePackageInfoModel? info = x switch
                {
                    { Package: not null, Thumbnail: not null } => new(x.Model,
                                                                      x.Package.Label,
                                                                      x.Package.Namespace,
                                                                      x.Package.ProjectId,
                                                                      x.Package.ProjectName,
                                                                      new
                                                                          InstancePackageVersionModel(x.Package
                                                                                 .VersionId,
                                                                              x.Package.VersionName,
                                                                              string.Join(",",
                                                                                  x.Package.Requirements
                                                                                     .AnyOfLoaders
                                                                                     .Select(LoaderHelper
                                                                                         .ToDisplayName)),
                                                                              string.Join(",",
                                                                                  x.Package.Requirements
                                                                                     .AnyOfVersions),
                                                                              x.Package.PublishedAt,
                                                                              x.Package.ReleaseType,
                                                                              x.Package.Dependencies)
                                                                      {
                                                                          IsCurrent = true
                                                                      },
                                                                      x.Package.Author,
                                                                      x.Package.Summary,
                                                                      x.Package.Reference,
                                                                      x.Thumbnail,
                                                                      x.Package.Kind),
                    { Project: not null, Thumbnail: not null } => new(x.Model,
                                                                      x.Project.Label,
                                                                      x.Project.Namespace,
                                                                      x.Project.ProjectId,
                                                                      x.Project.ProjectName,
                                                                      InstancePackageUnspecifiedVersionModel.Default,
                                                                      x.Project.Author,
                                                                      x.Project.Summary,
                                                                      x.Project.Reference,
                                                                      x.Thumbnail,
                                                                      x.Project.Kind),
                    _ => null
                };

                x.Model.OldPrefCache = x.Model.Entry.Pref;
                x.Model.Info = info;
                x.Model.IsLoaded = true;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex.Message,
                                           LanguageManager.Instance.InstanceSetupPage_ParsePrefDangerNotificationTitle.Current(),
                                           GrowlLevel.Danger,
                                           thumbnail: GetNotificationThumbnail());
        }
    }

    private async Task RefreshGroupInfoAsync(IReadOnlyList<GroupModel> groups, CancellationToken token)
    {
        foreach (var g in groups)
        {
            g.IsLoaded = false;
        }

        var identifiable = new List<(GroupModel Group, ProjectIdentifier Id)>();
        foreach (var g in groups)
        {
            if (g.Source is not null && PackageHelper.TryParse(g.Source, out var r))
            {
                identifiable.Add((g, r.ToProjectIdentifier()));
            }
        }

        try
        {
            token.ThrowIfCancellationRequested();
            if (identifiable.Count > 0)
            {
                var byId = identifiable.ToDictionary(x => x.Id, x => x.Group);
                var projects = await dataService.QueryProjectsAsync(identifiable.Select(x => x.Id));
                foreach (var (id, project) in projects.Successful)
                {
                    if (byId.TryGetValue(id, out var g))
                    {
                        g.Info = new ModpackGroupInfoModel(project.ProjectName, project.Thumbnail);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load modpack group info");
        }

        foreach (var g in groups)
        {
            g.IsLoaded = true;
        }
    }

    private Uri GetNotificationThumbnail(Uri? preferred = null) =>
        preferred
     ?? (Reference?.Value is InstanceReferenceModel { Thumbnail: { } thumbnail }
             ? thumbnail
             : ThumbnailHelper.ForInstance(Basic.Key));

    #endregion

    #region Fields

    private CancellationToken? _lifetimeToken;
    private CancellationTokenSource? _pageCancellationTokenSource;
    private readonly SourceCache<PackageListItemBase, PackageListKey> _flat = new(x => x.Key);
    private Task _metadataTask = Task.CompletedTask;
    private IDisposable? _updatingSubscription;
    private readonly CompositeDisposable _subscriptions = new();
    private readonly Dictionary<(PackageSourceHelper.Kind Kind, string? Source), GroupModel> _groupModels = new();
    private readonly LooseGroupModel _loose = new() { Kind = PackageSourceHelper.Kind.Manual, Source = null };

    #endregion

    #region Overrides

    protected override void OnModelUpdated(string key, Profile profile)
    {
        base.OnModelUpdated(key, profile);
        if (profile.Setup.Loader is not null && LoaderHelper.TryParse(profile.Setup.Loader, out var result))
        {
            LoaderLabel = LoaderHelper.ToDisplayLabel(result.Identity, result.Version);
        }
        else
        {
            LoaderLabel = "Enum_None";
        }

        // NOTE: 正在 Update/Deploy 期间也照常触发这些刷新（有意为之）。
        Dispatcher.UIThread.Post(() =>
        {
            TriggerPackageMerge();
            TriggerReferenceRefresh();
            NotifyGroupCommandStates();
            Rules ??= [with(profile.Setup.Rules, x => new(x), x => x.Owner)];
        });

        UpdatingPending = true;
        UpdatingProgress = 0;
    }

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        _lifetimeToken = token;
        _pageCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        var packages = _flat
                      .Connect()
                      .Filter(item => item is PackageListItemBase.Entry)
                      .Transform(item => ((PackageListItemBase.Entry)item).Package);

        packages
           .MergeManyChangeSets(x => x.Tags.ToObservableChangeSet())
           .GroupOn(x => x)
           .Transform(group => new InstancePackageFilterTagModel(group.GroupKey) { RefCount = group.List.Count })
           .DisposeMany()
           .Bind(out var tagsView)
           .Subscribe()
           .DisposeWith(_subscriptions);
        TagsView = tagsView;

        tagsView
           .ToObservableChangeSet()
           .AutoRefresh(x => x.IsSelected)
           .Filter(x => x.IsSelected)
           .Transform(x => x.Content)
           .Bind(out var filterTags)
           .Subscribe()
           .DisposeWith(_subscriptions);

        var text = this.WhenValueChanged(x => x.FilterText).Select(BuildTextFilter);
        var enability = this.WhenValueChanged(x => x.FilterEnability).Select(BuildEnabilityFilter);
        var lockility = this.WhenValueChanged(x => x.FilterLockility).Select(BuildLockilityFilter);
        var kind = this.WhenValueChanged(x => x.FilterKind).Select(BuildKindFilter);
        var tags = filterTags.ToObservableChangeSet().Select(_ => BuildTagFilter(filterTags));

        var sourceOrders = ProfileManager.TryGetImmutable(Basic.Key, out var profileForOrders)
                               ? profileForOrders.Setup.SourceOrders
                               : Array.Empty<string>();
        var comparer = new PackageListItemComparer(sourceOrders);

        var packageFilter = enability
                           .CombineLatest(lockility, (a, b) => (Func<InstancePackageModel, bool>)(x => a(x) && b(x)))
                           .CombineLatest(kind, (ab, c) => (Func<InstancePackageModel, bool>)(x => ab(x) && c(x)))
                           .CombineLatest(tags, (abc, d) => (Func<InstancePackageModel, bool>)(x => abc(x) && d(x)))
                           .CombineLatest(text, (abcd, e) => (Func<InstancePackageModel, bool>)(x => abcd(x) && e(x)));

        packages.QueryWhenChanged(items => items.Count).Subscribe(c => StageCount = c).DisposeWith(_subscriptions);
        _flat
           .Connect()
           .Filter(item => item is PackageListItemBase.Entry)
           .Transform(item => (PackageListItemBase.Entry)item)
           .QueryWhenChanged(query => query.Items.GroupBy(e => e.Group).ToDictionary(g => g.Key, g => g.Count()))
           .Subscribe(counts =>
            {
                foreach (var (group, count) in counts)
                {
                    group.Count = count;
                }
            })
           .DisposeWith(_subscriptions);

        packages
           .Filter(packageFilter)
           .QueryWhenChanged(items => items.Count)
           .Subscribe(c => FilteredCount = c)
           .DisposeWith(_subscriptions);

        var itemFilter =
            packageFilter.Select(pf => (Func<PackageListItemBase, bool>)(item => item is PackageListItemBase.Header
                                                                             || (item is PackageListItemBase.Entry e
                                                                                         && pf(e.Package))));

        _flat
           .Connect()
           .Filter(itemFilter)
           .AutoRefreshOnObservable(item => item.Group.WhenPropertyChanged(g => g.IsExpanded))
           .Filter(item => item is PackageListItemBase.Header || item.Group.IsExpanded)
           .SortAndBind(out var flatView, comparer)
           .Subscribe()
           .DisposeWith(_subscriptions);
        FlatView = flatView;

        filterTags
           .ToObservableChangeSet()
           .Select(_ => filterTags.Any())
           .CombineLatest(this.WhenValueChanged(x => x.FilterEnability).Select(x => x is { Value: not null }),
                          (x, y) => x || y)
           .CombineLatest(this.WhenValueChanged(x => x.FilterLockility).Select(x => x is { Value: not null }),
                          (x, y) => x || y)
           .CombineLatest(this.WhenValueChanged(x => x.FilterKind).Select(x => x is { Value: not null }),
                          (x, y) => x || y)
           .Subscribe(x => IsFilterActive = x)
           .DisposeWith(_subscriptions);

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        _pageCancellationTokenSource?.Cancel();
        _pageCancellationTokenSource?.Dispose();
        _pageCancellationTokenSource = null;
        _subscriptions.Dispose();
        return Task.CompletedTask;
    }

    #endregion

    #region Instance State

    protected override void OnInstanceUpdating(UpdateTracker tracker)
    {
        if (_pageCancellationTokenSource is null)
        {
            return;
        }
        IsRefreshing = false;
        _pageCancellationTokenSource.Cancel();
        _pageCancellationTokenSource.Dispose();
        _pageCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken!.Value);
        TrackUpdateProgress(tracker);
        base.OnInstanceUpdating(tracker);
    }

    protected override void OnInstanceUpdated(UpdateTracker tracker)
    {
        _updatingSubscription?.Dispose();
        if (_pageCancellationTokenSource is null || _pageCancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        base.OnInstanceUpdated(tracker);
    }

    private void TrackUpdateProgress(UpdateTracker update)
    {
        _updatingSubscription?.Dispose();
        _updatingSubscription = update
                               .ProgressStream.Sample(TimeSpan.FromSeconds(1))
                               .Subscribe(x =>
                                {
                                    UpdatingProgress = x ?? 0d;
                                    UpdatingPending = !x.HasValue;
                                })
                               .DisposeWith(update);
    }

    #endregion

    #region Filters

    private static Func<InstancePackageModel, bool> BuildEnabilityFilter(FilterModel? enablity) =>
        x => enablity?.Value switch
        {
            bool it => x.IsEnabled == it,
            _ => true
        };

    private static Func<InstancePackageModel, bool> BuildLockilityFilter(FilterModel? lockility) =>
        x => lockility?.Value switch
        {
            bool it => x.CanRemove != it,
            _ => true
        };

    private static Func<InstancePackageModel, bool> BuildKindFilter(FilterModel? kind) =>
        x => kind?.Value switch
        {
            ResourceKind it => x.Info?.Kind == it,
            _ => true
        };

    private static Func<InstancePackageModel, bool> BuildTextFilter(string? filter) =>
        x => string.IsNullOrEmpty(filter)
          || (x.Info is
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

    #endregion

    #region Commands

    [RelayCommand]
    private async Task EditLoaderAsync()
    {
        string? loader = null;
        string? version = null;
        if (Basic.Loader is not null && LoaderHelper.TryParse(Basic.Loader, out var result))
        {
            loader = result.Identity;
            version = result.Version;
        }

        var dialog = new LoaderEditorDialog
        {
            OverlayService = overlayService,
            DataService = dataService,
            GameVersion = Basic.Version,
            SelectedLoader = loader,
            SelectedVersion = version
        };
        if (await overlayService.PopDialogAsync(dialog))
        {
            if (ProfileManager.TryGetMutable(Basic.Key, out var guard))
            {
                if (dialog.Result is LoaderCandidateSelectionModel selection)
                {
                    var old = guard.Value.Setup.Loader;
                    var lurl = LoaderHelper.ToLurl(selection.Id, selection.Version);
                    guard.Value.Setup.Loader = lurl;
                    if (old != lurl)
                    {
                        persistenceService.AppendAction(new()
                        {
                            Key = Basic.Key,
                            Kind = PersistenceService.ActionKind.EditLoader,
                            Old = old,
                            New = lurl
                        });
                    }
                }
                else
                {
                    var old = guard.Value.Setup.Loader;
                    guard.Value.Setup.Loader = null;
                    if (old != null)
                    {
                        persistenceService.AppendAction(new()
                        {
                            Key = Basic.Key,
                            Kind = PersistenceService.ActionKind.EditLoader,
                            Old = old
                        });
                    }
                }

                await guard.DisposeAsync();
            }
        }
    }

    [RelayCommand]
    private void EditRules()
    {
        if (Rules is not null)
        {
            overlayService.PopModal(new ProfileRulesModal
            {
                Rules = Rules,
                Packages =
                [
                    .. _flat
                      .Items.OfType<PackageListItemBase.Entry>()
                      .Select(i => i.Package)
                ],
                OverlayService = overlayService
            });
        }
    }

    [RelayCommand]
    private void ViewPackage(InstancePackageModel? model)
    {
        if (model is { Info: not null } && ProfileManager.TryGetMutable(Basic.Key, out var guard))
        {
            overlayService.PopModal(new InstancePackageModal
            {
                DataContext = model.Info,
                Guard = guard,
                DataService = dataService,
                OverlayService = overlayService,
                PersistenceService = persistenceService,
                PackageMaterializer = packageMaterializer,
                Collection = _flat,
                NotificationService = notificationService,
                PackagePlanner = serviceProvider.GetRequiredService<PackagePlanner>(),
                Filter = new(Kind: model.Info.Kind,
                             Version: Basic.Version,
                             Loader: Basic.Loader is not null
                                         ? LoaderHelper.TryParse(Basic.Loader,
                                                                 out var result)
                                               ? result.Identity
                                               : null
                                         : null)
            });
        }
    }

    [RelayCommand]
    private async Task ViewDetails()
    {
        if (Basic.Source is not null && PackageHelper.TryParse(Basic.Source, out var source))
        {
            try
            {
                var project = await dataService.QueryProjectAsync(source.ToProjectIdentifier());
                var model = new ExhibitModpackModel(project.Label,
                                                    project.Namespace,
                                                    project.ProjectId,
                                                    project.ProjectName,
                                                    project.Author,
                                                    project.Reference,
                                                    project.Thumbnail ?? AssetUriIndex.DirtImage,
                                                    project.Tags,
                                                    project.DownloadCount,
                                                    project.Summary,
                                                    project.UpdatedAt,
                                                    [.. project.Gallery.Select(x => x.Url)]);
                overlayService.PopToast(new ExhibitModpackToast
                {
                    DataService = dataService,
                    PersistenceService = persistenceService,
                    DataContext = model,
                    InstallCommand = InstallVersionCommand
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex,
                                               LanguageManager.Instance.InstanceSetupPage_LoadProjectInformationDangerNotificationTitle.Current(),
                                               GrowlLevel.Warning,
                                               GetNotificationThumbnail());
            }
        }
    }

    [RelayCommand]
    private void GotoExplorerPage() =>
        navigationService.Navigate<ExplorerPage>(new InstanceExplorerSession(Basic.Key,
                                                                             ProfileManager,
                                                                             dataService,
                                                                             overlayService,
                                                                             persistenceService));

    [RelayCommand]
    private void GotoDependencyGraph() => overlayService.PopModal<InstanceDependencyGraphModal>(Basic);

    [RelayCommand]
    private async Task UpdateBatchAsync()
    {
        if (ProfileManager.TryGetImmutable(Basic.Key, out var profile))
        {
            var existingTags = profile.Setup.Packages.SelectMany(x => x.Tags).Distinct().OrderBy(t => t).ToList();
            var previewer = new PackageBulkUpdatePreviewerDialog
            {
                ExistingTags = existingTags,
                OverlayService = overlayService,
                IsEnabledOnly = true,
                ViewState = ViewState
            };
            if (await overlayService.PopDialogAsync(previewer)
             && previewer.Result is PackageBulkUpdatePreviewerModel
             {
                 IsEnabledOnly: var enabledOnly, TagPolicy: var tagPolicy, Tags: var tags
             })
            {
                var staging = _flat
                             .Items.OfType<PackageListItemBase.Entry>()
                             .Select(i => i.Package)
                             .Where(x => !enabledOnly || x.IsEnabled)
                             .Where(x => tagPolicy switch
                              {
                                  PackageBulkUpdatePreviewerTagPolicy.Include => tags.Any(y => x.Tags.Contains(y)),
                                  PackageBulkUpdatePreviewerTagPolicy.Exclude => !tags.Any(y => x.Tags.Contains(y)),
                                  _ => true
                              })
                             .ToList();
                var total = staging.Count;
                var progress =
                    notificationService.PopProgress(LanguageManager.Instance.InstanceSetupPage_PackageBulkUpdatingProgressingNotificationMessage.Current()
                                                   .Replace("{0}", "0")
                                                   .Replace("{1}", staging.Count.ToString()),
                                                    LanguageManager.Instance.InstanceSetupPage_PackageBulkUpdatingProgressingNotificationTitle.Current(),
                                                    thumbnail: GetNotificationThumbnail());

                progress.AddAction(new(LanguageManager.Instance.InstanceSetupPage_PackageBulkUpdatingProgressingNotificationCancelText.Current(),
                                       new RelayCommand(Cancel)));

                var filter = new Filter(Kind: null,
                                        Version: profile.Setup.Version,
                                        Loader: LoaderHelper.TryParse(profile.Setup.Loader, out var loader)
                                                    ? loader.Identity
                                                    : null);

                var updates = new ConcurrentBag<PackageBulkUpdateReviewerModel>();
                try
                {
                    // NOTE: 并发度设 2 是上限，再大触发 Modrinth API 限流。
                    var semaphore = new SemaphoreSlim(2);
                    // NOTE: 无法用批量查询优化——ResolveBatch 不带版本限制会拉全量版本再筛选。
                    // ReSharper disable once AccessToDisposedClosure
                    var tasks = staging.Select(x => UpdateAsync(x, semaphore, progress));
                    await Task.Run(async () => await Task.WhenAll(tasks));
                    semaphore.Dispose();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    notificationService.PopMessage(ex,
                                                   LanguageManager.Instance.InstanceSetupPage_LoadProjectInformationDangerNotificationTitle.Current(),
                                                   GrowlLevel.Warning,
                                                   GetNotificationThumbnail());
                }

                if (progress.Token.IsCancellationRequested)
                {
                    return;
                }

                // NOTE: 用 Dismiss 会让 Token 置 Cancel、Notification 显示“过期”、Growl 直接消失，
                //  故这里直接 Dispose。
                progress.Dispose();

                notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_PackageBulkUpdatingProgressedNotificationTitle.Current(),
                                               LanguageManager.Instance.InstanceSetupPage_PackageBulkUpdatingProgressedNotificationMessage.Current()
                                                  .Replace("{0}", updates.Count.ToString()),
                                               thumbnail: GetNotificationThumbnail(),
                                               actions: new
                                                   GrowlAction(LanguageManager.Instance.InstanceSetupPage_PackageBulkUpdatingProgressedNotificationReviewText.Current(),
                                                               new AsyncRelayCommand(ReviewAsync, CanReview)));
                return;

                async Task UpdateAsync(
                    InstancePackageModel entry,
                    SemaphoreSlim semaphore,
                    NotificationService.ProgressHandle handle)
                {
                    if (handle.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    await semaphore.WaitAsync(handle.Token);
                    if (entry.CanUpdate && PackageHelper.TryParse(entry.Entry.Pref, out var result))
                    {
                        if (result.Version is not null)
                        {
                            try
                            {
                                var resolved = await dataService
                                                    .ResolvePackageAsync(new(result.Repository,
                                                                             result.Namespace,
                                                                             result.Identity,
                                                                             null),
                                                                         filter,
                                                                         false)
                                                    .ConfigureAwait(false);
                                if (resolved.VersionId != result.Version)
                                {
                                    var package = await dataService
                                                       .ResolvePackageAsync(result, Filter.None)
                                                       .ConfigureAwait(false);
                                    var model = new PackageBulkUpdateReviewerModel(entry,
                                        package,
                                        package.Thumbnail ?? AssetUriIndex.DirtImage,
                                        package.VersionId,
                                        package.VersionName,
                                        package.PublishedAt,
                                        resolved.VersionId,
                                        resolved.VersionName,
                                        resolved.PublishedAt);
                                    updates.Add(model);
                                }
                            }
                            catch (Exception ex)
                            {
                                notificationService.PopMessage(ex,
                                                               entry.Info?.ProjectName ?? entry.Entry.Pref,
                                                               GrowlLevel.Warning,
                                                               GetNotificationThumbnail());
                            }
                        }
                    }

                    Interlocked.Decrement(ref total);
                    semaphore.Release();

                    Dispatcher.UIThread.Post(() =>
                    {
                        handle.Report(Math.Min(100d, 100d * (StageCount - total) / StageCount));
                        handle.Report(LanguageManager.Instance.InstanceSetupPage_PackageBulkUpdatingProgressingNotificationMessage.Current()
                                     .Replace("{0}", updates.Count.ToString())
                                     .Replace("{1}", total.ToString()));
                    });
                }

                void Cancel()
                {
                    if (!progress.IsDisposed)
                    {
                        progress.Dispose();
                    }
                }

                bool CanReview() => !updates.IsEmpty;

                async Task ReviewAsync()
                {
                    // NOTE: 直接改不可变 Profile 引用的性能 Trick：Profile 是单例，改动已生效，
                    //  只是不经过 guard.DisposeAsync 落盘。不用 Guard 是避免无效刷新 diff，且本流程
                    //  跨三个控制流、可在任意层中断，Guard 无法保证释放而可能泄露。缺陷：进程被杀时
                    //  批量更新可能未保存到硬盘。
                    var dialog = new PackageBulkUpdateReviewerDialog { Result = updates.ToList() };
                    if (await overlayService.PopDialogAsync(dialog)
                     && dialog.Result is IReadOnlyList<PackageBulkUpdateReviewerModel> results)
                    {
                        foreach (var model in results.Where(x => x.IsChecked))
                        {
                            var old = model.Model.Entry.Pref;
                            model.Model.Info?.Version = new InstancePackageVersionModel(model.NewVersionId,
                                model.NewVersionName,
                                string.Join(",",
                                            model.Package.Requirements.AnyOfLoaders.Select(LoaderHelper
                                               .ToDisplayName)),
                                string.Join(",", model.Package.Requirements.AnyOfVersions),
                                model.NewVersionTimeRaw,
                                model.Package.ReleaseType,
                                model.Package.Dependencies);
                            // NOTE: 给 Info.Version 赋值会同步写回 Entry.Pref。
                            persistenceService.AppendAction(new()
                            {
                                Key = Basic.Key,
                                Kind = PersistenceService.ActionKind.EditPackage,
                                Old = old,
                                New = model.Model.Entry.Pref
                            });
                        }
                    }
                }
            }
        }
    }

    [RelayCommand]
    private async Task ImportListAsync()
    {
        var filePath = await overlayService.RequestFileAsync();
        if (File.Exists(filePath))
        {
            try
            {
                var addedCount = 0;
                var updatedCount = 0;
                var failedCount = 0;
                var pendingTagUpdates = new List<(Profile.Rice.Entry Entry, List<string> ToAdd)>();
                await Task.Run(async () =>
                {
                    var importedEntries = new List<ExportedEntry>();

                    using (var reader = new StreamReader(filePath))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        await foreach (var record in csv.GetRecordsAsync<ExportedEntry>())
                        {
                            importedEntries.Add(record);
                        }
                    }

                    if (importedEntries.Count == 0)
                    {
                        notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_ImportListNoPackagesWarningNotificationMessage.Current(),
                                                       LanguageManager.Instance.InstanceSetupPage_ImportListWarningNotificationTitle.Current(),
                                                       GrowlLevel.Warning,
                                                       thumbnail: GetNotificationThumbnail());
                        return;
                    }

                    if (ProfileManager.TryGetMutable(Basic.Key, out var guard))
                    {
                        await using (guard)
                        {
                            foreach (var importedEntry in importedEntries)
                            {
                                try
                                {
                                    if (string.IsNullOrEmpty(importedEntry.Pref))
                                    {
                                        failedCount++;
                                        continue;
                                    }

                                    Profile.Rice.Entry? existingEntry = null;
                                    if (PackageHelper.TryParse(importedEntry.Pref, out var importedPref))
                                    {
                                        existingEntry =
                                            guard.Value.Setup.Packages.FirstOrDefault(x =>
                                                PackageHelper.IsMatched(x.Pref,
                                                                        importedPref.Repository,
                                                                        importedPref.Namespace,
                                                                        importedPref.Identity));
                                    }

                                    if (existingEntry != null)
                                    {
                                        var oldPref = existingEntry.Pref;
                                        existingEntry.Pref = importedEntry.Pref;
                                        existingEntry.Enabled = importedEntry.Enabled;

                                        var tags = importedEntry
                                                  .Tags.Split('|')
                                                  .Where(x => !string.IsNullOrEmpty(x))
                                                  .ToList();
                                        var toAdd = tags.Except(existingEntry.Tags).ToList();

                                        if (toAdd.Count > 0)
                                        {
                                            pendingTagUpdates.Add((existingEntry, toAdd));
                                        }

                                        if (oldPref != importedEntry.Pref)
                                        {
                                            persistenceService.AppendAction(new()
                                            {
                                                Key = Basic.Key,
                                                Kind = PersistenceService.ActionKind
                                                                         .EditPackage,
                                                Old = oldPref,
                                                New = importedEntry.Pref
                                            });
                                        }

                                        updatedCount++;
                                    }
                                    else
                                    {
                                        var newEntry = new Profile.Rice.Entry
                                        {
                                            Enabled = importedEntry.Enabled,
                                            Pref = importedEntry.Pref,
                                            Source = importedEntry.Source
                                        };
                                        guard.Value.Setup.Packages.Add(newEntry);
                                        persistenceService.AppendAction(new()
                                        {
                                            Key = Basic.Key,
                                            Kind = PersistenceService.ActionKind
                                                                     .EditPackage,
                                            New = importedEntry.Pref
                                        });
                                        addedCount++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.LogError(ex, "Failed to import package: {pref}", importedEntry.Pref);
                                    failedCount++;
                                }
                            }
                        }
                    }
                });

                // HACK: 触发更新与 Entry 修改都不会同步 Tags，需要手动推送到 InstancePackageModel；
                //  Tags 是 UI 绑定的 ObservableCollection，必须在 UI 线程修改
                foreach (var (entry, toAdd) in pendingTagUpdates)
                {
                    var item = _flat.Lookup(new PackageListKey.Entry(entry));
                    if (item.HasValue)
                    {
                        foreach (var tag in toAdd)
                        {
                            ((PackageListItemBase.Entry)item.Value).Package.Tags.Add(tag);
                        }
                    }
                }

                var resultMessage = LanguageManager.Instance.InstanceSetupPage_ImportListSuccessNotificationMessage.Current()
                                   .Replace("{0}", addedCount.ToString())
                                   .Replace("{1}", updatedCount.ToString())
                                   .Replace("{2}", failedCount.ToString());
                var level = failedCount > 0 ? GrowlLevel.Warning : GrowlLevel.Success;
                notificationService.PopMessage(resultMessage,
                                               LanguageManager.Instance.InstanceSetupPage_ImportListSuccessNotificationTitle.Current(),
                                               level,
                                               thumbnail: GetNotificationThumbnail());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to import package list from file: {path}", filePath);
                notificationService.PopMessage(ex,
                                               LanguageManager.Instance.InstanceSetupPage_ImportListDangerNotificationTitle.Current(),
                                               thumbnail: GetNotificationThumbnail());
            }
        }
    }

    private async Task ApplyRecipeAsync(string recipeId)
    {
        var recipe = persistenceService.GetRecipe(recipeId);
        if (recipe is null)
        {
            return;
        }

        var items = persistenceService.GetRecipeItems(recipeId);
        if (items.Count == 0)
        {
            notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_ApplyRecipeEmptyWarningNotificationMessage.Current(),
                                           LanguageManager.Instance.InstanceSetupPage_ImportListWarningNotificationTitle.Current(),
                                           GrowlLevel.Warning,
                                           thumbnail: GetNotificationThumbnail());
            return;
        }

        var addedCount = 0;

        try
        {
            await Task.Run(async () =>
            {
                var identifiers = items
                                 .Select(i => new PackageIdentifier(i.Label,
                                                                    PersistenceService.NormalizeNamespace(i.Namespace),
                                                                    i.ProjectId,
                                                                    null))
                                 .ToList();
                var filter = Filter.FromSetup(ProfileManager.GetImmutable(Basic.Key).Setup);
                var resolved = await dataService.ResolvePackagesAsync(identifiers, filter);

                var resolvedByProject =
                    resolved.Successful.ToDictionary(x => (x.Key.Repository.ToLowerInvariant(), x.Key.Namespace,
                                                           x.Key.Identity),
                                                     x => x.Value);

                if (ProfileManager.TryGetMutable(Basic.Key, out var guard))
                {
                    await using (guard)
                    {
                        var source = RecipeHelper.ToUri(recipeId);
                        var setup = guard.Value.Setup;
                        foreach (var item in items)
                        {
                            var ns = PersistenceService.NormalizeNamespace(item.Namespace);
                            var package =
                                resolvedByProject.TryGetValue((item.Label.ToLowerInvariant(), ns, item.ProjectId),
                                                              out var p)
                                    ? p
                                    : null;
                            var pref = package is not null
                                           ? PackageHelper.ToPref(package)
                                           : PackageHelper.ToPref(item.Label, ns, item.ProjectId, null);
                            var tags = JsonSerializer.Deserialize<List<string>>(item.Tags) ?? [];

                            setup.Packages.Add(new()
                            {
                                Pref = pref,
                                Enabled = package is not null,
                                Source = source,
                                Tags = tags
                            });
                            persistenceService.AppendAction(new()
                            {
                                Key = Basic.Key,
                                Kind = PersistenceService.ActionKind.EditPackage,
                                New = pref
                            });
                            addedCount++;
                        }
                    }
                }
            });

            notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_ApplyRecipeSuccessNotificationMessage.Current()
                                                    .Replace("{0}", addedCount.ToString()),
                                           LanguageManager.Instance.InstanceSetupPage_ApplyRecipeSuccessNotificationTitle.Current(),
                                           GrowlLevel.Success,
                                           thumbnail: GetNotificationThumbnail());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply recipe {id}", recipeId);
            notificationService.PopMessage(ex,
                                           LanguageManager.Instance.InstanceSetupPage_ApplyRecipeDangerNotificationTitle.Current(),
                                           thumbnail: GetNotificationThumbnail());
        }
    }

    [RelayCommand]
    private async Task AddToCollectionAsync(InstancePackageModel? pkg) => await AssignToCollectionAsync(pkg);

    [RelayCommand]
    private async Task MoveToCollectionAsync(InstancePackageModel? pkg) => await AssignToCollectionAsync(pkg);

    private async Task AssignToCollectionAsync(InstancePackageModel? pkg)
    {
        if (pkg?.Entry is null)
        {
            return;
        }

        var existing = ProfileManager.TryGetImmutable(Basic.Key, out var p)
            ? p.Setup.Packages
               .Select(e => e.Source)
               .OfType<string>()
               .Where(s => InternalUriHelper.IsKind(s, CollectionHelper.SCHEME))
               .Select(s => CollectionHelper.TryGetName(s, out var n) ? new CollectionModel(n, s) : null)
               .OfType<CollectionModel>()
               .Distinct()
               .ToList()
            : new();

        var dialog = new CollectionPickerDialog { ExistingCollections = existing };
        if (!await overlayService.PopDialogAsync(dialog) || dialog.Result is not CollectionModel collection)
        {
            return;
        }

        pkg.Entry.Source = collection.Uri;

        // HACK: item.Group init-only 不可变，改 Source 后须先移除旧 item 让 TriggerPackageMerge 走新增分支重新归组
        //  这里用反查的方式查询当前操作的 InstancePackageModel 所属的 PackageListItemBase 属于脆弱操作
        var stale = _flat.Items.OfType<PackageListItemBase.Entry>().FirstOrDefault(i => ReferenceEquals(i.Package, pkg));
        if (stale is not null)
        {
            _flat.Remove([stale.Key]);
        }
        TriggerPackageMerge();
    }

    [RelayCommand]
    private void  RemoveFromCollection(InstancePackageModel? model)
    {
        if (model?.Entry is null
            || PackageSourceHelper.Classify(model.Entry.Source) != PackageSourceHelper.Kind.Collection)
        {
            return;
        }

        model.Entry.Source = null;

        var stale = _flat.Items.OfType<PackageListItemBase.Entry>().FirstOrDefault(i => ReferenceEquals(i.Package, model));
        if (stale is not null)
        {
            _flat.Remove([stale.Key]);
        }

        TriggerPackageMerge();
    }

    [RelayCommand]
    private async Task ImportFromRecipeAsync()
    {
        var recipes = persistenceService.GetRecipes();
        if (recipes.Count == 0)
        {
            notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_ImportRecipeNoRecipesWarningNotificationMessage.Current(),
                                           LanguageManager.Instance.InstanceSetupPage_ImportListWarningNotificationTitle.Current(),
                                           GrowlLevel.Warning,
                                           thumbnail: GetNotificationThumbnail());
            return;
        }

        var source = recipes
                    .Select(r => new RecipeCardModel(r.Id) { Name = r.Name, Description = r.Description })
                    .ToList();
        var dialog = new RecipePickerDialog { RecipesSource = source };
        if (await overlayService.PopDialogAsync(dialog) && dialog.Result is RecipeCardModel selected)
        {
            await ApplyRecipeAsync(selected.Id);
        }
    }

    [RelayCommand]
    private async Task ExportListAsync()
    {
        var profile = ProfileManager.GetImmutable(Basic.Key);
        var dialog = new PackageListExporterDialog { PackageCount = profile.Setup.Packages.Count, Key = Basic.Key };
        if (await overlayService.PopDialogAsync(dialog) && dialog.Result is string path)
        {
            var output = new List<ExportedEntry>();
            var progress = notificationService.PopProgress("Export package list to file",
                                                           thumbnail: GetNotificationThumbnail());
            var items = profile
                       .Setup.Packages
                       .Select(entry => PackageHelper.TryParse(entry.Pref, out var parsed)
                                            ? (Entry: entry, Id: parsed)
                                            : ((Profile.Rice.Entry Entry, PackageIdentifier Id)?)null)
                       .Where(x => x is not null)
                       .Select(x => x!.Value)
                       .ToList();

            try
            {
                var successful = new Dictionary<(PackageIdentifier Id, string? Source), Package>();
                var failed = new Dictionary<(PackageIdentifier Id, string? Source), Exception>();

                foreach (var sourceGroup in items.GroupBy(x => x.Entry.Source))
                {
                    var result =
                        await dataService.ResolvePackagesAsync(sourceGroup.Select(x => x.Id).Distinct(), Filter.None);
                    foreach (var (id, package) in result.Successful)
                    {
                        successful[(id, sourceGroup.Key)] = package;
                    }

                    foreach (var (id, error) in result.Failed)
                    {
                        failed[(id, sourceGroup.Key)] = error;
                    }
                }

                new BatchResult<(PackageIdentifier Id, string? Source), Package>(successful, failed)
                   .ThrowIfFailures();

                foreach (var item in items)
                {
                    var pkg = successful[(item.Id, item.Entry.Source)];
                    output.Add(new(item.Entry.Pref,
                                   pkg.Label,
                                   pkg.Namespace,
                                   pkg.ProjectId,
                                   pkg.VersionId,
                                   item.Entry.Enabled,
                                   item.Entry.Source,
                                   pkg.ProjectName,
                                   item.Id.Version is not null ? pkg.VersionName : null,
                                   string.Join("|", item.Entry.Tags)));
                }
            }
            catch (BatchResultException<(PackageIdentifier Id, string? Source)> ex)
            {
                var failed = ex
                            .Failures.Keys
                            .SelectMany(key => items
                                              .Where(x => x.Id == key.Id && x.Entry.Source == key.Source)
                                              .Select(x => x.Entry.Pref))
                            .Distinct()
                            .ToArray();
                notificationService.PopMessage(string.Join(Environment.NewLine, failed),
                                               LanguageManager.Instance.InstanceSetupPage_FetchingInformationDangerNotificationTitle.Current(),
                                               GrowlLevel.Warning,
                                               thumbnail: GetNotificationThumbnail());
                progress.Dispose();
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to export");
                notificationService.PopMessage(ex,
                                               LanguageManager.Instance.InstanceSetupPage_FetchingInformationDangerNotificationTitle.Current(),
                                               GrowlLevel.Warning,
                                               GetNotificationThumbnail());
                progress.Dispose();
                return;
            }

            progress.Dispose();

            try
            {
                await Task.Run(() =>
                {
                    var dir = Path.GetDirectoryName(path);
                    if (dir != null && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    using var writer = new StreamWriter(path);
                    using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
                    csv.WriteRecords(output);
                });

                notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_ExportListSuccessNotificationMessage.Current()
                                                        .Replace("{0}", path),
                                               LanguageManager.Instance.InstanceSetupPage_ExportListSuccessNotificationTitle.Current(),
                                               GrowlLevel.Success,
                                               thumbnail: GetNotificationThumbnail());
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_ExportListDangerNotificationMessage.Current()
                                              .Replace("{0}", path)
                                              .Replace("{1}", ex.Message),
                                               LanguageManager.Instance.InstanceSetupPage_ExportListDangerNotificationTitle.Current(),
                                               GrowlLevel.Danger,
                                               thumbnail: GetNotificationThumbnail());
            }
        }
    }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        if (Reference is { Value: InstanceReferenceModel reference }
         && PackageHelper.TryParse(reference.Pref, out var result))
        {
            try
            {
                var page = await dataService.InspectVersionsAsync(result.Repository,
                                                                  result.Namespace,
                                                                  result.Identity,
                                                                  Filter.None with
                                                                  {
                                                                      Kind = ResourceKind.Modpack,
                                                                      Version = Basic.Version
                                                                  });
                var versions = page
                              .Select(x => new InstanceReferenceVersionModel(x.Label,
                                                                             x.Namespace,
                                                                             x.ProjectId,
                                                                             x.VersionId,
                                                                             x.VersionName,
                                                                             x.ReleaseType,
                                                                             x.PublishedAt)
                              {
                                  IsCurrent = x.VersionId == reference.VersionId
                              })
                              .ToList();
                var dialog = new ReferenceVersionPickerDialog { Versions = versions };
                if (await overlayService.PopDialogAsync(dialog)
                 && dialog.Result is InstanceReferenceVersionModel version)
                {
                    Update(version);
                }
            }
            catch (ApiException ex)
            {
                logger.LogError(ex, "Failed to check update: {}", reference.Pref);
                notificationService.PopMessage(ex,
                                               LanguageManager.Instance.InstanceSetupPage_CheckUpdateDangerNotificationTitle.Current(),
                                               thumbnail: GetNotificationThumbnail(reference.Thumbnail));
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to check update: {}", reference.Pref);
                notificationService.PopMessage(ex,
                                               LanguageManager.Instance.InstanceSetupPage_CheckUpdateDangerNotificationTitle.Current(),
                                               thumbnail: GetNotificationThumbnail(reference.Thumbnail));
            }
        }
    }

    private bool CanUpdate(InstanceReferenceVersionModel? model) => model is { IsCurrent: false };

    [RelayCommand(CanExecute = nameof(CanUpdate))]
    private void Update(InstanceReferenceVersionModel? model)
    {
        if (model is null)
        {
            return;
        }

        try
        {
            InstanceManager.Update(Basic.Key, model.Label, model.Namespace, model.Pid, model.Vid);
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex,
                                           LanguageManager.Instance.InstanceSetupPage_UpdateDangerNotificationTitle.Current(),
                                           thumbnail: GetNotificationThumbnail());
        }
    }

    [RelayCommand]
    private void InstallVersion(ExhibitVersionModel? version)
    {
        if (version is not null)
        {
            InstanceManager.Install(version.ProjectName,
                                    version.Label,
                                    version.Namespace,
                                    version.ProjectId,
                                    version.VersionId);
            notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_InstallVersionNotificationMessage.Current()
                                          .Replace("{0}", version.ProjectName)
                                          .Replace("{1}", version.VersionName),
                                           thumbnail: GetNotificationThumbnail());
        }
    }

    [RelayCommand]
    private async Task RemovePackageAsync(InstancePackageModel? model)
    {
        if (model is not null && ProfileManager.TryGetMutable(Basic.Key, out var guard))
        {
            await using (guard)
            {
                guard.Value.Setup.Packages.Remove(model.Entry);
                persistenceService.AppendAction(new()
                {
                    Key = Basic.Key,
                    Kind = PersistenceService.ActionKind.EditPackage,
                    Old = model.Entry.Pref
                });
            }
        }
    }

    [RelayCommand]
    private async Task BatchEnableAsync()
    {
        var candidates = _flat
                        .Items.OfType<PackageListItemBase.Entry>()
                        .Where(i => !i.Package.IsEnabled)
                        .Select(i => new SelectablePackageModel(i.Package, i.Key))
                        .ToList();
        if (candidates.Count == 0)
        {
            notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_BatchEnableNothingNotificationMessage.Current(),
                                           LanguageManager.Instance.InstanceSetupPage_BatchManagementNotificationTitle.Current(),
                                           GrowlLevel.Warning,
                                           thumbnail: GetNotificationThumbnail());
            return;
        }

        var dialog = new PackageSelectorDialog { Intent = PackageSelectorDialog.SelectionIntent.Enable };
        dialog.SetItems(candidates);
        if (await overlayService.PopDialogAsync(dialog)
         && dialog.Result is IReadOnlyList<SelectablePackageModel> { Count: > 0 } selected)
        {
            foreach (var item in selected)
            {
                item.Source.IsEnabled = true;
            }

            notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_BatchEnableSucceededNotificationMessage.Current()
                                                    .Replace("{0}", selected.Count.ToString()),
                                           LanguageManager.Instance.InstanceSetupPage_BatchManagementNotificationTitle.Current(),
                                           GrowlLevel.Success,
                                           thumbnail: GetNotificationThumbnail());
        }
    }

    [RelayCommand]
    private async Task BatchDisableAsync()
    {
        var candidates = _flat
                        .Items.OfType<PackageListItemBase.Entry>()
                        .Where(i => i.Package.IsEnabled)
                        .Select(i => new SelectablePackageModel(i.Package, i.Key))
                        .ToList();
        if (candidates.Count == 0)
        {
            notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_BatchDisableNothingNotificationMessage.Current(),
                                           LanguageManager.Instance.InstanceSetupPage_BatchManagementNotificationTitle.Current(),
                                           GrowlLevel.Warning,
                                           thumbnail: GetNotificationThumbnail());
            return;
        }

        var dialog = new PackageSelectorDialog { Intent = PackageSelectorDialog.SelectionIntent.Disable };
        dialog.SetItems(candidates);
        if (await overlayService.PopDialogAsync(dialog)
         && dialog.Result is IReadOnlyList<SelectablePackageModel> { Count: > 0 } selected)
        {
            foreach (var item in selected)
            {
                item.Source.IsEnabled = false;
            }

            notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_BatchDisableSucceededNotificationMessage.Current()
                                                    .Replace("{0}", selected.Count.ToString()),
                                           LanguageManager.Instance.InstanceSetupPage_BatchManagementNotificationTitle.Current(),
                                           GrowlLevel.Success,
                                           thumbnail: GetNotificationThumbnail());
        }
    }

    [RelayCommand]
    private async Task BatchDeleteAsync()
    {
        var candidates = _flat
                        .Items.OfType<PackageListItemBase.Entry>()
                        .Where(i => i.Package.CanRemove)
                        .Select(i => new SelectablePackageModel(i.Package, i.Key))
                        .ToList();
        if (candidates.Count == 0)
        {
            notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_BatchRemoveNothingNotificationMessage.Current(),
                                           LanguageManager.Instance.InstanceSetupPage_BatchManagementNotificationTitle.Current(),
                                           GrowlLevel.Warning,
                                           thumbnail: GetNotificationThumbnail());
            return;
        }

        var dialog = new PackageSelectorDialog { Intent = PackageSelectorDialog.SelectionIntent.Remove };
        dialog.SetItems(candidates);
        if (await overlayService.PopDialogAsync(dialog)
         && dialog.Result is IReadOnlyList<SelectablePackageModel> { Count: > 0 } selected)
        {
            if (!await overlayService.RequestStrongConfirmationAsync(LanguageManager.Instance.InstanceSetupPage_BatchRemoveConfirmMessage.Current()
                                                                    .Replace("{0}", selected.Count.ToString()),
                                                                     LanguageManager.Instance.InstanceSetupPage_BatchRemoveConfirmTitle.Current()))
            {
                return;
            }

            if (ProfileManager.TryGetImmutable(Basic.Key, out var profile))
            {
                var keys = new List<PackageListKey>();
                foreach (var item in selected)
                {
                    profile.Setup.Packages.Remove(item.Source.Entry);
                    keys.Add(item.Key);
                    persistenceService.AppendAction(new()
                    {
                        Key = Basic.Key,
                        Kind = PersistenceService.ActionKind.EditPackage,
                        Old = item.Source.Entry.Pref
                    });
                }

                TriggerPackageMerge();

                notificationService.PopMessage(LanguageManager.Instance.InstanceSetupPage_BatchRemoveSucceededNotificationMessage.Current()
                                                        .Replace("{0}", selected.Count.ToString()),
                                               LanguageManager.Instance.InstanceSetupPage_BatchManagementNotificationTitle.Current(),
                                               GrowlLevel.Success,
                                               thumbnail: GetNotificationThumbnail());
            }
        }
    }

    [RelayCommand]
    private void RefreshPackages() => TriggerPackageMerge();

    [RelayCommand]
    private async Task EditRaw(InstancePackageModel? model)
    {
        if (model == null)
        {
            return;
        }

        var res = await overlayService.RequestInputAsync(placeholder: model.Entry.Pref);
        if (res != null)
        {
            model.Entry.Pref = res;
            TriggerPackageMerge();
        }
    }

    [RelayCommand]
    private void ToggleGroupExpanded(GroupModel? group)
    {
        if (group is null)
        {
            return;
        }

        group.IsExpanded = !group.IsExpanded;
    }

    [RelayCommand]
    private void EnableGroup(GroupModel? group) => SetGroupEnabled(group, true);

    [RelayCommand]
    private void DisableGroup(GroupModel? group) => SetGroupEnabled(group, false);

    private void SetGroupEnabled(GroupModel? group, bool value)
    {
        if (group is null)
        {
            return;
        }

        foreach (var item in _flat.Items.OfType<PackageListItemBase.Entry>().Where(i => i.Group == group))
        {
            item.Package.IsEnabled = value;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDisbandGroup))]
    private async Task DisbandGroupAsync(GroupModel? group)
    {
        if (group?.Source is null || !PackageSourceHelper.CanUngroup(group.Source, Basic.Source))
        {
            return;
        }

        var items = _flat.Items.OfType<PackageListItemBase.Entry>().Where(i => i.Group == group).ToList();
        if (items.Count == 0)
        {
            return;
        }

        if (!await overlayService.RequestStrongConfirmationAsync(LanguageManager.Instance.InstanceSetupPage_DisbandGroupConfirmMessage.Current(),
                                                                 LanguageManager.Instance.InstanceSetupPage_DisbandGroupConfirmTitle.Current()))
        {
            return;
        }

        foreach (var item in items)
        {
            item.Package.Entry.Source = null;
        }

        // HACK: merge 按 Entry 地址判存续，Source 置空后 Entry 仍在 profile 中，已存在的 item 不会重建，
        //  而 item.Group 是 init-only 不可变——只能先删旧 item，让 merge 走新增分支重新归组到散装
        _flat.Remove(items.Select(i => i.Key));
        TriggerPackageMerge();
    }

    private bool CanDisbandGroup(GroupModel? group) =>
        group is { Source: not null } && PackageSourceHelper.CanUngroup(group.Source, Basic.Source);

    [RelayCommand(CanExecute = nameof(CanRemoveGroup))]
    private async Task RemoveGroupAsync(GroupModel? group)
    {
        if (group?.Source is null || !PackageSourceHelper.CanDelete(group.Source, Basic.Source))
        {
            return;
        }

        var items = _flat.Items.OfType<PackageListItemBase.Entry>().Where(i => i.Group == group).ToList();
        if (items.Count == 0)
        {
            return;
        }

        if (!await overlayService.RequestStrongConfirmationAsync(LanguageManager.Instance.InstanceSetupPage_RemoveGroupConfirmMessage.Current(),
                                                                 LanguageManager.Instance.InstanceSetupPage_RemoveGroupConfirmTitle.Current()))
        {
            return;
        }

        if (ProfileManager.TryGetImmutable(Basic.Key, out var profile))
        {
            foreach (var item in items)
            {
                profile.Setup.Packages.Remove(item.Package.Entry);
                persistenceService.AppendAction(new()
                {
                    Key = Basic.Key,
                    Kind = PersistenceService.ActionKind.EditPackage,
                    Old = item.Package.Entry.Pref
                });
            }

            TriggerPackageMerge();
        }
    }

    private bool CanRemoveGroup(GroupModel? group) =>
        group is { Source: not null } && PackageSourceHelper.CanDelete(group.Source, Basic.Source);

    [RelayCommand(CanExecute = nameof(CanPromoteToRecipe))]
    private async Task PromoteToRecipeAsync(GroupModel? group)
    {
        if (group is not { Kind: PackageSourceHelper.Kind.Collection, Source: not null }
            || !CollectionHelper.TryGetName(group.Source, out var name))
        {
            return;
        }

        var items = _flat.Items.OfType<PackageListItemBase.Entry>().Where(i => i.Group == group).ToList();
        if (items.Count == 0)
        {
            return;
        }

        if (!await overlayService.RequestConfirmationAsync(
                LanguageManager.Instance.InstanceSetupPage_PromoteToRecipeConfirmMessage.Current(),
                LanguageManager.Instance.InstanceSetupPage_PromoteToRecipeConfirmTitle.Current()))
        {
            return;
        }

        var recipe = persistenceService.InsertRecipe(name, null);
        var source = RecipeHelper.ToUri(recipe.Id);
        foreach (var item in items)
        {
            if (PackageHelper.TryParse(item.Package.Entry.Pref, out var id))
            {
                persistenceService.AddRecipeItem(recipe.Id,
                                                 new(id.Repository, id.Namespace, id.Identity),
                                                 [.. item.Package.Entry.Tags],
                                                 null);
            }

            item.Package.Entry.Source = source;
        }

        _flat.Remove(items.Select(i => i.Key));
        TriggerPackageMerge();
    }

    private bool CanPromoteToRecipe(GroupModel? group) =>
        group is { Kind: PackageSourceHelper.Kind.Collection, Source: not null };

    [RelayCommand(CanExecute = nameof(CanDemoteToCollection))]
    private async Task DemoteToCollectionAsync(GroupModel? group)
    {
        if (group is not { Kind: PackageSourceHelper.Kind.Recipe, Source: not null })
        {
            return;
        }

        var items = _flat.Items.OfType<PackageListItemBase.Entry>().Where(i => i.Group == group).ToList();
        if (items.Count == 0)
        {
            return;
        }

        var recipe = persistenceService.GetRecipe(RecipeHelper.GetId(group.Source));
        if (recipe is null)
        {
            return;
        }

        if (!await overlayService.RequestConfirmationAsync(
                LanguageManager.Instance.InstanceSetupPage_DemoteToCollectionConfirmMessage.Current(),
                LanguageManager.Instance.InstanceSetupPage_DemoteToCollectionConfirmTitle.Current()))
        {
            return;
        }

        var source = CollectionHelper.ToUri(recipe.Name);
        foreach (var item in items)
        {
            item.Package.Entry.Source = source;
        }

        _flat.Remove(items.Select(i => i.Key));
        TriggerPackageMerge();
    }

    private bool CanDemoteToCollection(GroupModel? group) =>
        group is { Kind: PackageSourceHelper.Kind.Recipe, Source: not null };

    // NOTE: SourceOrders 末项 = 最高覆盖力（POLY-116），因此提升优先级表现为向列表尾部移动
    [RelayCommand(CanExecute = nameof(CanRaiseGroupPriority))]
    private async Task RaiseGroupPriorityAsync(GroupModel? group) => await MoveGroupAsync(group, +1);

    [RelayCommand(CanExecute = nameof(CanLowerGroupPriority))]
    private async Task LowerGroupPriorityAsync(GroupModel? group) => await MoveGroupAsync(group, -1);

    private bool CanRaiseGroupPriority(GroupModel? group) => CanMoveGroup(group, +1);

    private bool CanLowerGroupPriority(GroupModel? group) => CanMoveGroup(group, -1);

    private bool CanMoveGroup(GroupModel? group, int delta)
    {
        if (group?.Source is null)
        {
            return false;
        }

        var order = BuildGroupOrder();
        var index = order.FindIndex(g => ReferenceEquals(g, group));
        var target = index + delta;
        return index >= 0 && target >= 0 && target < order.Count;
    }

    private async Task MoveGroupAsync(GroupModel? group, int delta)
    {
        if (group?.Source is null)
        {
            return;
        }

        var order = BuildGroupOrder();
        var index = order.FindIndex(g => ReferenceEquals(g, group));
        if (index < 0)
        {
            return;
        }

        var target = index + delta;
        if (target < 0 || target >= order.Count)
        {
            return;
        }

        (order[index], order[target]) = (order[target], order[index]);

        if (ProfileManager.TryGetMutable(Basic.Key, out var guard))
        {
            // NOTE: 未入列的组随首次移动显式化进 SourceOrders（列进即声明显式覆盖层，POLY-116），
            //  因此移动总是落全量新序，而非只交换已列项
            var orders = guard.Value.Setup.SourceOrders;
            orders.Clear();
            foreach (var source in order.Select(g => g.Source!))
            {
                orders.Add(source);
            }

            await guard.DisposeAsync();
        }
    }

    private List<GroupModel> BuildGroupOrder()
    {
        if (!ProfileManager.TryGetImmutable(Basic.Key, out var profile))
        {
            return [];
        }

        var headers = _flat.Items.OfType<PackageListItemBase.Header>().ToList();
        headers.Sort(new PackageListItemComparer(profile.Setup.SourceOrders));
        return [.. headers.Select(h => h.Group)];
    }

    private void NotifyGroupCommandStates()
    {
        DisbandGroupCommand.NotifyCanExecuteChanged();
        RemoveGroupCommand.NotifyCanExecuteChanged();
        RaiseGroupPriorityCommand.NotifyCanExecuteChanged();
        LowerGroupPriorityCommand.NotifyCanExecuteChanged();
        PromoteToRecipeCommand.NotifyCanExecuteChanged();
        DemoteToCollectionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task ActivateGroupHeaderAsync(GroupModel? group)
    {
        if (group is null)
        {
            return;
        }

        if (group.Info is null)
        {
            TriggerPackageMerge();
            return;
        }

        await ViewGroupDetailsAsync(group);
    }

    [RelayCommand]
    private async Task ViewGroupDetailsAsync(GroupModel? group)
    {
        if (group is { Kind: PackageSourceHelper.Kind.Recipe, Source: not null })
        {
            navigationService.Navigate<RecipePage>(RecipeHelper.GetId(group.Source));
            return;
        }

        if (group is { Kind: PackageSourceHelper.Kind.Modpack }
         && group.Source is not null
         && PackageHelper.TryParse(group.Source, out var source))
        {
            try
            {
                var project = await dataService.QueryProjectAsync(source.ToProjectIdentifier());
                var model = new ExhibitModpackModel(project.Label,
                                                    project.Namespace,
                                                    project.ProjectId,
                                                    project.ProjectName,
                                                    project.Author,
                                                    project.Reference,
                                                    project.Thumbnail ?? AssetUriIndex.DirtImage,
                                                    project.Tags,
                                                    project.DownloadCount,
                                                    project.Summary,
                                                    project.UpdatedAt,
                                                    [.. project.Gallery.Select(x => x.Url)]);
                overlayService.PopToast(new ExhibitModpackToast
                {
                    DataService = dataService,
                    PersistenceService = persistenceService,
                    DataContext = model,
                    InstallCommand = InstallVersionCommand
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex,
                                               LanguageManager.Instance.InstanceSetupPage_LoadProjectInformationDangerNotificationTitle.Current(),
                                               GrowlLevel.Warning,
                                               GetNotificationThumbnail());
            }
        }
    }

    private static (PackageSourceHelper.Kind Kind, string? Source) GetGroupKey(InstancePackageModel pkg) =>
        (PackageSourceHelper.Classify(pkg.Entry.Source), pkg.Entry.Source);

    private GroupModel GroupModelOf(InstancePackageModel pkg) => GroupModelOf(GetGroupKey(pkg));

    private GroupModel GroupModelOf((PackageSourceHelper.Kind Kind, string? Source) key)
    {
        if (key.Kind == PackageSourceHelper.Kind.Manual)
        {
            return _loose;
        }

        return EnsureGroup(key.Kind, key.Source!);
    }

    private GroupModel EnsureGroup(PackageSourceHelper.Kind kind, string source)
    {
        if (!_groupModels.TryGetValue((kind, source), out var model))
        {
            var g = new GroupModel { Kind = kind, Source = source };
            if (kind == PackageSourceHelper.Kind.Recipe)
            {
                // NOTE: Recipe 信息同步可得——能解析即赋 Info，解析不出则 Info 留空，
                //  与 Modpack 网络 IO 失败合并为同一「Info 未赋值 = 失败」语义，交公共层渲染重试。
                g.IsLoaded = true;
                var recipe = persistenceService.GetRecipe(RecipeHelper.GetId(source));
                if (recipe is not null)
                {
                    g.Info = new RecipeGroupInfoModel(recipe.Name);
                }
            }
            if (kind == PackageSourceHelper.Kind.Collection && CollectionHelper.TryGetName(source, out var collectionName))
            {
                g.IsLoaded = true;
                g.Info = new CollectionGroupInfoModel(collectionName);
            }

            _groupModels[(kind, source)] = g;
            return g;
        }

        return model;
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial LazyObject? Reference { get; set; }

    [ObservableProperty]
    public partial string LoaderLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int StageCount { get; set; }

    [ObservableProperty]
    public partial int FilteredCount { get; set; }

    [ObservableProperty]
    public partial double UpdatingProgress { get; set; }

    [ObservableProperty]
    public partial bool UpdatingPending { get; set; } = true;

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; } = false;

    [ObservableProperty]
    public partial ReadOnlyObservableCollection<PackageListItemBase>? FlatView { get; set; }

    [ObservableProperty]
    public partial ReadOnlyObservableCollection<InstancePackageFilterTagModel>? TagsView { get; set; }

    [ObservableProperty]
    public partial MappingCollection<Profile.Rice.Rule, ProfileRuleModel>? Rules { get; set; }

    [ObservableProperty]
    public partial string? FilterText { get; set; }

    [ObservableProperty]
    public partial FilterModel? FilterEnability { get; set; }

    [ObservableProperty]
    public partial FilterModel? FilterLockility { get; set; }

    [ObservableProperty]
    public partial FilterModel? FilterKind { get; set; }

    [ObservableProperty]
    public partial bool IsFilterActive { get; set; }

    [ObservableProperty]
    public partial StateView? ViewState { get; set; }

    public string ViewStateKey => Basic.Key;

    #endregion
}
