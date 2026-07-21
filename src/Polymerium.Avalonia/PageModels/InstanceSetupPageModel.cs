using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
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
using Polymerium.Avalonia.Properties;
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
    PersistenceService persistenceService) : InstancePageModelBase(context, aggregator, instanceManager, profileManager),
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
            // Entry 是 class，按地址比较；仍存在的包不动其 Entry 项（实例稳定），
            // 信息是否陈旧由 RefreshMetadataAsync 现场重判，这里不预判
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

            StageCount = entryCount;
            _flat.Remove(toRemove);
            var persistentIndex = entryCount - toRemove.Count;
            var toAdd = lookup
                       .Select(x =>
                        {
                            var pkg = new InstancePackageModel(x,
                                                               PackageSourceHelper.CanUpdate(x.Source, Basic.Source))
                            {
                                PersistentIndex = persistentIndex++,
                            };
                            return new PackageListItemBase.Entry
                            {
                                Key = new PackageListKey.Entry(x),
                                Group = GroupModelOf(pkg),
                                Package = pkg,
                            };
                        })
                       .ToList();
            _flat.AddOrUpdate(toAdd);
            // _flat 是合并期的唯一写入对象，无外部并发修改，StageCount 与 profile 严格一致

            // 同步组头：为每个有成员的非散装组确保恰好一个 Header，移除空组的 Header。
            var presentGroups = _flat.Items
                                     .OfType<PackageListItemBase.Entry>()
                                     .Select(i => i.Group)
                                     .Where(g => g is not LooseGroupModel)
                                     .Distinct()
                                     .ToList();

            var bySource = presentGroups.ToDictionary(g => g.Source!, g => g);
            var desiredKeys = bySource.Keys.Select(s => new PackageListKey.Header(s)).ToHashSet();
            var currentKeys = _flat.Keys.OfType<PackageListKey.Header>().ToHashSet();
            _flat.Remove(currentKeys.Except(desiredKeys).Cast<PackageListKey>().ToList());
            foreach (var key in desiredKeys.Except(currentKeys))
            {
                _flat.AddOrUpdate(new PackageListItemBase.Header { Key = key, Group = bySource[key.Source] });
            }

            StageCount += toAdd.Count - toRemove.Count;
            if (StageCount != profile.Setup.Packages.Count)
            {
                throw new UnreachableException("使用相对数量更新总数是很大胆冒险的，但能很好验证差异计算是否正确。能触发这个异常就是差异计算出现错误了");
            }

            // 统一入口：包与组的信息加载排成一队，RefreshMetadataAsync 现场重判待加载项、统一管 IsRefreshing
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

        // Basic 是 InstancePageModel 维护的，理论上会先在 ProfileUpdated 时更新，但不可靠
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
                                                                                        Kind = ResourceKind.Modpack,
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

    // 包与组信息加载的统一入口：排队执行（不取消），现场重判待加载项；统一管理 IsRefreshing 与异常。
    private async Task RefreshMetadataAsync(Task previous, CancellationToken token)
    {
        // 排队：等上一个完成；吞掉其异常，避免 faulted 任务卡死整条队列
        try
        {
            await previous;
        }
        catch
        {
        }

        token.ThrowIfCancellationRequested();

        // 现场重判：排到时若前一个已把事情做完，这里为空，直接 no-op 完成
        var pendingPackages = _flat.Items
                                  .OfType<PackageListItemBase.Entry>()
                                  .Select(i => i.Package)
                                  .Where(p => p.Info is null || p.OldPrefCache != p.Entry.Pref)
                                  .ToList();
        var pendingGroups = _flat.Items
                                 .OfType<PackageListItemBase.Entry>()
                                 .Select(i => i.Group)
                                 .OfType<ModpackGroupModel>()
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
                               RefreshModpackGroupInfoAsync(pendingGroups, token));
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
                package.IsLoaded = false;

            var items = packages
                       .Select(x => PackageHelper.TryParse(x.Entry.Pref, out var pref)
                                        ? (Model: x,
                                           Pref: pref,
                                           Data: new RefreshIntermediateData(x))
                                        : throw new FormatException($"Failed to parse pref: {x.Entry.Pref}"))
                       .ToList();

            foreach (var sourceGroup in items.GroupBy(x => x.Model.Entry.Source))
            {
                token.ThrowIfCancellationRequested();

                var known = sourceGroup.Where(x => x.Pref.Version is not null).ToArray();
                if (known.Length > 0)
                {
                    var resolved = await dataService.ResolvePackagesAsync(
                        known.Select(x => x.Pref).Distinct(),
                        Filter.None);
                    foreach (var (id, package) in resolved.Successful)
                    {
                        foreach (var item in known.Where(x => x.Pref == id))
                            item.Data.Package = package;
                    }
                }

                var unknown = sourceGroup.Where(x => x.Pref.Version is null).ToArray();
                if (unknown.Length > 0)
                {
                    var queried = await dataService.QueryProjectsAsync(
                        unknown
                           .Select(x => x.Pref.ToProjectIdentifier())
                           .Distinct());
                    foreach (var (projectKey, project) in queried.Successful)
                    {
                        var id = projectKey.ToPackageIdentifier();
                        foreach (var item in unknown.Where(x => x.Pref == id))
                            item.Data.Project = project;
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
                                                                          IsCurrent = true,
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
                    _ => null,
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
                                           Resources.InstanceSetupPage_ParsePrefDangerNotificationTitle,
                                           GrowlLevel.Danger,
                                           thumbnail: GetNotificationThumbnail());
        }
    }

    private async Task RefreshModpackGroupInfoAsync(IReadOnlyList<ModpackGroupModel> groups, CancellationToken token)
    {
        foreach (var g in groups)
            g.IsLoaded = false;

        var identifiable = new List<(ModpackGroupModel Group, ProjectIdentifier Id)>();
        foreach (var g in groups)
        {
            if (g.Source is not null && PackageHelper.TryParse(g.Source, out var r))
                identifiable.Add((g, r.ToProjectIdentifier()));
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
                        g.Info = new(g, project.ProjectName, project.Thumbnail);
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
            g.IsLoaded = true;
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
    private readonly Dictionary<(PackageSourceHelper.Kind Kind, string? Source), GroupModelBase> _groupModels = new();
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
            LoaderLabel = Resources.Enum_None;
        }

        // 即使正在 Update 或 Deploy 也会 Trigger
        Dispatcher.UIThread.Post(() =>
        {
            TriggerPackageMerge();
            TriggerReferenceRefresh();
            // 只需要一次，因为 Profile.Setup.Rules 总是同一个
            Rules ??= new(profile.Setup.Rules, x => new(x), x => x.Owner);
        });

        UpdatingPending = true;
        UpdatingProgress = 0;
    }

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        _lifetimeToken = token;
        _pageCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        // 包变更流（tags 与计数共用）：从拍平源取 Entry 项解包成 InstancePackageModel
        var packages = _flat
           .Connect()
           .Filter(item => item is PackageListItemBase.Entry)
           .Transform(item => ((PackageListItemBase.Entry)item).Package);

        packages
           .MergeManyChangeSets(x => x.Tags.ToObservableChangeSet())
           .GroupOn(x => x)
           .Transform(group => new InstancePackageFilterTagModel(group.GroupKey) { RefCount = group.List.Count, })
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

        // 计数：只数通过过滤的包，不受折叠与组头影响
        packages
           .Filter(packageFilter)
           .QueryWhenChanged(items => items.Count)
           .Subscribe(c => FilteredCount = c)
           .DisposeWith(_subscriptions);

        // 列表：组头永远放行（独立于过滤），Entry 才套包过滤；折叠只藏 Entry 不藏组头
        var itemFilter = packageFilter.Select(pf => (Func<PackageListItemBase, bool>)(item =>
             item is PackageListItemBase.Header || item is PackageListItemBase.Entry e && pf(e.Package)));

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
        _subscriptions.Dispose();
        return Task.CompletedTask;
    }

    #endregion

    #region Instance State

    protected override void OnInstanceUpdating(UpdateTracker tracker)
    {
        IsRefreshing = false;
        _pageCancellationTokenSource?.Cancel();
        _pageCancellationTokenSource?.Dispose();
        _pageCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken!.Value);
        TrackUpdateProgress(tracker);
        base.OnInstanceUpdating(tracker);
    }

    protected override void OnInstanceUpdated(UpdateTracker tracker)
    {
        _updatingSubscription?.Dispose();
        if (_pageCancellationTokenSource is null || _pageCancellationTokenSource.IsCancellationRequested)
        // NOTE: 当 TokenSource 被销毁意味着该页面已经退出
        //  但该 TrackerBase.StateChanged 事件未接触订阅
        //  实际是状态订阅有三层，第一层由 InstancePageModelBase 维护，且正确工作
        //  第二层是第一层的订阅事件中创建，由事件处理函数维护
        //  而第三层是位于 TrackerBase 内部，这一层状态维护脱离 ViewModel 但是状态表现却在 ViewModel 中进行
        //  需要减少数据链路的层数，让整个状态可统一维护，例如使用统一的状态收发 StateAggregator
        {
            return;
        }

        // OnModelUpdated 会负责刷新列表
        // TriggerRefresh(_pageCancellationTokenSource.Token);
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
            _ => true,
        };

    private static Func<InstancePackageModel, bool> BuildLockilityFilter(FilterModel? lockility) =>
        x => lockility?.Value switch
        {
            bool it => x.CanRemove != it,
            _ => true,
        };

    private static Func<InstancePackageModel, bool> BuildKindFilter(FilterModel? kind) =>
        x => kind?.Value switch
        {
            ResourceKind it => x.Info?.Kind == it,
            _ => true,
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
                  _ => name.Contains(y, StringComparison.OrdinalIgnoreCase),
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
            SelectedVersion = version,
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
                            New = lurl,
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
                            Old = old,
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
                Packages = _flat.Items.OfType<PackageListItemBase.Entry>().Select(i => i.Package).ToList(),
                OverlayService = overlayService,
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
                                         : null),
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
                    InstallCommand = InstallVersionCommand,
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex,
                                               Resources
                                                  .InstanceSetupPage_LoadProjectInformationDangerNotificationTitle,
                                               GrowlLevel.Warning,
                                               thumbnail: GetNotificationThumbnail());
            }
        }
    }

    [RelayCommand]
    private void GotoPackageExplorerPage() => navigationService.Navigate<PackageExplorerPage>(Basic.Key);

    [RelayCommand]
    private void GotoDependencyGraph()
        => overlayService.PopModal<InstanceDependencyGraphModal>(Basic);

    [RelayCommand]
    private async Task UpdateBatchAsync()
    {
        if (ProfileManager.TryGetImmutable(Basic.Key, out var profile))
        {
            // 收集所有现有标签（去重，排除当前包已有的标签）
            var existingTags = profile.Setup.Packages.SelectMany(x => x.Tags).Distinct().OrderBy(t => t).ToList();
            var previewer = new PackageBulkUpdatePreviewerDialog()
            {
                ExistingTags = existingTags,
                OverlayService = overlayService,
                IsEnabledOnly = true,
                ViewState = ViewState,
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
                                  _ => true,
                              })
                             .ToList();
                var total = staging.Count;
                var progress =
                    notificationService.PopProgress(Resources
                                                   .InstanceSetupPage_PackageBulkUpdatingProgressingNotificationMessage
                                                   .Replace("{0}", "0")
                                                   .Replace("{1}", staging.Count.ToString()),
                                                    Resources
                                                       .InstanceSetupPage_PackageBulkUpdatingProgressingNotificationTitle,
                                                    thumbnail: GetNotificationThumbnail());

                progress.AddAction(new(Resources.InstanceSetupPage_PackageBulkUpdatingProgressingNotificationCancelText,
                                       new RelayCommand(Cancel)));

                var filter = new Filter(Kind: null,
                                        Version: profile.Setup.Version,
                                        Loader: LoaderHelper.TryParse(profile.Setup.Loader, out var loader)
                                                    ? loader.Identity
                                                    : null);

                var updates = new ConcurrentBag<PackageBulkUpdateReviewerModel>();
                try
                {
                    // 值设置太大会触发 API 限制
                    var semaphore = new SemaphoreSlim(2);
                    // 这里无法使用批量查询来优化，ResolveBatch 无版本限制会 Fallback 到获取所有版本并筛选合适的，这个无法避免
                    // ReSharper disable once AccessToDisposedClosure
                    var tasks = staging.Select(x => UpdateAsync(x, semaphore, progress));
                    await Task.Run(async () => await Task.WhenAll(tasks));
                    semaphore.Dispose();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    notificationService.PopMessage(ex,
                                                   Resources
                                                      .InstanceSetupPage_LoadProjectInformationDangerNotificationTitle,
                                                   GrowlLevel.Warning,
                                                   thumbnail: GetNotificationThumbnail());
                }

                if (progress.Token.IsCancellationRequested)
                {
                    return;
                }

                // 调用 Dismiss 会让 Token 进入 Cancel 状态，导致 Notification 显示“过期”，Growl 直接消失
                progress.Dispose();

                notificationService.PopMessage(Resources
                                                  .InstanceSetupPage_PackageBulkUpdatingProgressedNotificationTitle,
                                               Resources
                                                  .InstanceSetupPage_PackageBulkUpdatingProgressedNotificationMessage
                                                  .Replace("{0}", updates.Count.ToString()),
                                               thumbnail: GetNotificationThumbnail(),
                                               actions: new
                                                   GrowlAction(Resources
                                                                  .InstanceSetupPage_PackageBulkUpdatingProgressedNotificationReviewText,
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
                                                    .ResolvePackageAsync(new PackageIdentifier(result.Repository, result.Namespace, result.Identity, null),
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
                                                               thumbnail: GetNotificationThumbnail());
                            }
                        }
                    }

                    Interlocked.Decrement(ref total);
                    semaphore.Release();

                    Dispatcher.UIThread.Post(() =>
                    {
                        handle.Report(Math.Min(100d,
                                               100d * (StageCount - total) / StageCount));
                        handle.Report(Resources
                                     .InstanceSetupPage_PackageBulkUpdatingProgressingNotificationMessage
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
                    // 这里有个性能 Trick，使用的是不可变 Profile 引用，这里不使用 ProfileGuard 是为了避免重复刷新
                    // 由于 Profile 是单例的，实际改变已经被应用，只是没有用 guard.DisposeAsync 通知写入硬盘
                    // 为什么不用 Guard：
                    // 除了避免触发无效的刷新 diff 以外还有一个原因是这里涉及跨三个控制流且可以在任意一层中断
                    // 导致 Guard 无法保证能被释放而出现泄露
                    // 缺陷：可能会导致批量更新未能保存到硬盘，例如进程被杀的情况
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
                            // 设置 Version 会同步到 Entry.Pref
                            persistenceService.AppendAction(new()
                            {
                                Key = Basic.Key,
                                Kind = PersistenceService.ActionKind.EditPackage,
                                Old = old,
                                New = model.Model.Entry.Pref,
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

                    // 读取 CSV 文件
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
                        notificationService.PopMessage(Resources
                                                          .InstanceSetupPage_ImportListNoPackagesWarningNotificationMessage,
                                                       Resources.InstanceSetupPage_ImportListWarningNotificationTitle,
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

                                    // 查找是否已存在相同的包（基于 Label, Namespace, ProjectId）
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
                                        // 更新现有包的版本
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
                                                New = importedEntry.Pref,
                                            });
                                        }

                                        updatedCount++;
                                    }
                                    else
                                    {
                                        // 添加新包
                                        var newEntry = new Profile.Rice.Entry
                                        {
                                            Enabled = importedEntry.Enabled,
                                            Pref = importedEntry.Pref,
                                            Source = importedEntry.Source,
                                        };
                                        guard.Value.Setup.Packages.Add(newEntry);
                                        persistenceService.AppendAction(new()
                                        {
                                            Key = Basic.Key,
                                            Kind = PersistenceService.ActionKind
                                                                     .EditPackage,
                                            New = importedEntry.Pref,
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

                // 显示结果通知
                var resultMessage = Resources
                                   .InstanceSetupPage_ImportListSuccessNotificationMessage
                                   .Replace("{0}", addedCount.ToString())
                                   .Replace("{1}", updatedCount.ToString())
                                   .Replace("{2}", failedCount.ToString());
                var level = failedCount > 0 ? GrowlLevel.Warning : GrowlLevel.Success;
                notificationService.PopMessage(resultMessage,
                                               Resources.InstanceSetupPage_ImportListSuccessNotificationTitle,
                                               level,
                                               thumbnail: GetNotificationThumbnail());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to import package list from file: {path}", filePath);
                notificationService.PopMessage(ex,
                                               Resources.InstanceSetupPage_ImportListDangerNotificationTitle,
                                               thumbnail: GetNotificationThumbnail());
            }
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
            var items = profile.Setup.Packages
                               .Select(entry => PackageHelper.TryParse(entry.Pref, out var parsed)
                                                    ? (Entry: entry,
                                                       Id: parsed)
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
                    var result = await dataService.ResolvePackagesAsync(
                        sourceGroup.Select(x => x.Id).Distinct(),
                        Filter.None);
                    foreach (var (id, package) in result.Successful)
                        successful[(id, sourceGroup.Key)] = package;
                    foreach (var (id, error) in result.Failed)
                        failed[(id, sourceGroup.Key)] = error;
                }

                new BatchResolveResult<(PackageIdentifier Id, string? Source), Package>(successful, failed)
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
            catch (BatchResolveException<(PackageIdentifier Id, string? Source)> ex)
            {
                var failed = ex.Failures.Keys
                              .SelectMany(key => items
                                                .Where(x => x.Id == key.Id && x.Entry.Source == key.Source)
                                                .Select(x => x.Entry.Pref))
                              .Distinct()
                              .ToArray();
                notificationService.PopMessage(string.Join(Environment.NewLine, failed),
                                               Resources.InstanceSetupPage_FetchingInformationDangerNotificationTitle,
                                               GrowlLevel.Warning,
                                               thumbnail: GetNotificationThumbnail());
                progress.Dispose();
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to export");
                notificationService.PopMessage(ex,
                                               Resources.InstanceSetupPage_FetchingInformationDangerNotificationTitle,
                                               GrowlLevel.Warning,
                                               thumbnail: GetNotificationThumbnail());
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

                notificationService.PopMessage(Resources.InstanceSetupPage_ExportListSuccessNotificationMessage
                                                        .Replace("{0}", path),
                                               Resources.InstanceSetupPage_ExportListSuccessNotificationTitle,
                                               GrowlLevel.Success,
                                               thumbnail: GetNotificationThumbnail());
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(Resources
                                              .InstanceSetupPage_ExportListDangerNotificationMessage
                                              .Replace("{0}", path)
                                              .Replace("{1}", ex.Message),
                                               Resources.InstanceSetupPage_ExportListDangerNotificationTitle,
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
                                                                      Version = Basic.Version,
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
                                  IsCurrent = x.VersionId == reference.VersionId,
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
                                               Resources.InstanceSetupPage_CheckUpdateDangerNotificationTitle,
                                               thumbnail: GetNotificationThumbnail(reference.Thumbnail));
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to check update: {}", reference.Pref);
                notificationService.PopMessage(ex,
                                               Resources.InstanceSetupPage_CheckUpdateDangerNotificationTitle,
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
                                           Resources.InstanceSetupPage_UpdateDangerNotificationTitle,
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
            notificationService.PopMessage(Resources
                                          .InstanceSetupPage_InstallVersionNotificationMessage
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
                    Old = model.Entry.Pref,
                });
            }
        }
    }

    [RelayCommand]
    private async Task BatchEnableAsync()
    {
        var candidates = _flat.Items.OfType<PackageListItemBase.Entry>()
                              .Where(i => !i.Package.IsEnabled)
                              .Select(i => new SelectablePackageModel(i.Package, i.Key))
                              .ToList();
        if (candidates.Count == 0)
        {
            notificationService.PopMessage(Resources.InstanceSetupPage_BatchEnableNothingNotificationMessage,
                                           Resources.InstanceSetupPage_BatchManagementNotificationTitle,
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
                item.Source.IsEnabled = true;

            notificationService.PopMessage(
                Resources.InstanceSetupPage_BatchEnableSucceededNotificationMessage.Replace("{0}", selected.Count.ToString()),
                Resources.InstanceSetupPage_BatchManagementNotificationTitle,
                GrowlLevel.Success,
                thumbnail: GetNotificationThumbnail());
        }
    }

    [RelayCommand]
    private async Task BatchDisableAsync()
    {
        var candidates = _flat.Items.OfType<PackageListItemBase.Entry>()
                              .Where(i => i.Package.IsEnabled)
                              .Select(i => new SelectablePackageModel(i.Package, i.Key))
                              .ToList();
        if (candidates.Count == 0)
        {
            notificationService.PopMessage(Resources.InstanceSetupPage_BatchDisableNothingNotificationMessage,
                                           Resources.InstanceSetupPage_BatchManagementNotificationTitle,
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
                item.Source.IsEnabled = false;

            notificationService.PopMessage(
                Resources.InstanceSetupPage_BatchDisableSucceededNotificationMessage.Replace("{0}", selected.Count.ToString()),
                Resources.InstanceSetupPage_BatchManagementNotificationTitle,
                GrowlLevel.Success,
                thumbnail: GetNotificationThumbnail());
        }
    }

    [RelayCommand]
    private async Task BatchDeleteAsync()
    {
        var candidates = _flat.Items.OfType<PackageListItemBase.Entry>()
                              .Where(i => i.Package.CanRemove)
                              .Select(i => new SelectablePackageModel(i.Package, i.Key))
                              .ToList();
        if (candidates.Count == 0)
        {
            notificationService.PopMessage(Resources.InstanceSetupPage_BatchRemoveNothingNotificationMessage,
                                           Resources.InstanceSetupPage_BatchManagementNotificationTitle,
                                           GrowlLevel.Warning,
                                           thumbnail: GetNotificationThumbnail());
            return;
        }

        var dialog = new PackageSelectorDialog { Intent = PackageSelectorDialog.SelectionIntent.Remove };
        dialog.SetItems(candidates);
        if (await overlayService.PopDialogAsync(dialog)
         && dialog.Result is IReadOnlyList<SelectablePackageModel> { Count: > 0 } selected)
        {
            if (!await overlayService.RequestStrongConfirmationAsync(
                    Resources.InstanceSetupPage_BatchRemoveConfirmMessage.Replace("{0}", selected.Count.ToString()),
                    Resources.InstanceSetupPage_BatchRemoveConfirmTitle))
            {
                return;
            }

            // NOTE: 直接改单例 Packages 并外科式同步 _flat，不开 guard、不落盘、不触发 merge
            //  （落盘由页面生命周期负责，同 UpdateBatch.ReviewAsync）。
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
                        Old = item.Source.Entry.Pref,
                    });
                }
                _flat.Remove(keys);
                StageCount -= keys.Count;

                notificationService.PopMessage(
                    Resources.InstanceSetupPage_BatchRemoveSucceededNotificationMessage.Replace("{0}", selected.Count.ToString()),
                    Resources.InstanceSetupPage_BatchManagementNotificationTitle,
                    GrowlLevel.Success,
                    thumbnail: GetNotificationThumbnail());
            }
        }
    }

    [RelayCommand]
    private void RefreshPackages()
    {
        TriggerPackageMerge();
    }

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
    private void ToggleGroupExpanded(GroupModelBase? group)
    {
        if (group is null)
            return;
        group.IsExpanded = !group.IsExpanded;
    }

    [RelayCommand]
    private async Task ViewGroupDetailsAsync(GroupModelBase? group)
    {
        if (group is ModpackGroupModel && group.Source is not null
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
                    InstallCommand = InstallVersionCommand,
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex,
                                               Resources
                                                  .InstanceSetupPage_LoadProjectInformationDangerNotificationTitle,
                                               GrowlLevel.Warning,
                                               thumbnail: GetNotificationThumbnail());
            }
        }
    }

    private static (PackageSourceHelper.Kind Kind, string? Source) GetGroupKey(InstancePackageModel pkg) =>
        (PackageSourceHelper.Classify(pkg.Entry.Source), pkg.Entry.Source);

    private GroupModelBase GroupModelOf(InstancePackageModel pkg) => GroupModelOf(GetGroupKey(pkg));

    private GroupModelBase GroupModelOf((PackageSourceHelper.Kind Kind, string? Source) key)
    {
        if (key.Kind == PackageSourceHelper.Kind.Manual)
            return _loose;

        // NOTE: RecipeGroupModel 未设计，Recipe 系统落地后再实现（当前数据无 recipe:// source）
        if (key.Kind == PackageSourceHelper.Kind.Recipe)
            throw new NotImplementedException("Recipe group is not implemented yet.");

        return EnsureModpackGroup(key.Source!);
    }

    private ModpackGroupModel EnsureModpackGroup(string source)
    {
        if (!_groupModels.TryGetValue((PackageSourceHelper.Kind.Modpack, source), out var model))
        {
            model = new ModpackGroupModel { Kind = PackageSourceHelper.Kind.Modpack, Source = source };
            _groupModels[(PackageSourceHelper.Kind.Modpack, source)] = model;
        }

        return (ModpackGroupModel)model;
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
}
