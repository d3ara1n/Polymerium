using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Microsoft.Extensions.Logging;
using Polymerium.App.Assets;
using Polymerium.App.Dialogs;
using Polymerium.App.Facilities;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Properties;
using Polymerium.App.Services;
using Polymerium.App.Toasts;
using Polymerium.App.Views;
using Refit;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Trident.Core.Services;
using Trident.Core.Services.Instances;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;

namespace Polymerium.App.ViewModels;

public partial class InstanceSetupViewModel(
    ViewBag bag,
    ILogger<InstanceSetupViewModel> logger,
    ProfileManager profileManager,
    NotificationService notificationService,
    InstanceManager instanceManager,
    DataService dataService,
    OverlayService overlayService,
    NavigationService navigationService,
    PersistenceService persistenceService,
    ConfigurationService configurationService) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    #region Nested type: ExportedEntry

    private record ExportedEntry(
        string Purl,
        string? Label,
        string? Namespace,
        string? ProjectId,
        string? VersionId,
        bool Enabled,
        string? Source,
        string? Name,
        string? Version);

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
            // 计算 profile.Setup.Packages 与 _stageSource.Keys 的差异
            var lookup = profile.Setup.Packages.ToHashSet();
            var toRemove = new List<Profile.Rice.Entry>();
            var toUpdate = new List<InstancePackageModel>();
            foreach (var entry in _stageSource.Keys)
            {
                // actualValue 必定是同一个，因为 Entry 是 class，基于地址比较
                if (lookup.TryGetValue(entry, out _))
                {
                    var old = _stageSource.Lookup(entry).Value;
                    // old.Info is null 是考虑到有些上次加载还没完成就被打断，这次就继续加载
                    if (old.OldPurlCache != entry.Purl || old.Info is null)
                    {
                        toUpdate.Add(old);
                    }

                    lookup.Remove(entry);
                }
                else
                {
                    toRemove.Add(entry);
                }
            }

            StageCount = _stageSource.Count;
            _stageSource.Remove(toRemove);
            var toAdd = lookup
                       .Select(x => new InstancePackageModel(x, x.Source is not null && x.Source == Basic.Source))
                       .ToList();
            _stageSource.AddOrUpdate(toAdd);
            StageCount += toAdd.Count - toRemove.Count;
            if (StageCount != profile.Setup.Packages.Count)
            {
                // NOTE: 通过即时给 StageCount 赋值，以容忍两次 Merge 期间外部对 _stageSource 的修改
                throw new UnreachableException("使用相对数量更新总数是很大胆冒险的，但能很好验证差异计算是否正确。能触发这个异常就是差异计算出现错误了");
            }

            var toModify = toAdd.Concat(toUpdate).ToList();
            if (toModify.Count > 0)
            {
                _ = RefreshPackagesAsync(toModify, token.Value);
            }
        }
    }

    private void TriggerReferenceRefresh()
    {
        var token = _pageCancellationTokenSource?.Token;
        if (token is null || token.Value.IsCancellationRequested)
        {
            return;
        }

        // Basic 是 InstanceViewModel 维护的，理论上会先在 ProfileUpdated 时更新，但不可靠
        if (ProfileManager.TryGetImmutable(Basic.Key, out var profile))
        {
            if (profile.Setup.Source is not null)
            {
                if (Reference is null
                 || (Reference is { Value: InstanceReferenceModel { } reference }
                  && reference.Purl != profile.Setup.Source))
                {
                    if (PackageHelper.TryParse(profile.Setup.Source, out var r))
                    {
                        Reference = new(async _ =>
                        {
                            var package = await dataService.ResolvePackageAsync(r.Label,
                                                                                    r.Namespace,
                                                                                    r.Pid,
                                                                                    r.Vid,
                                                                                    Filter.None with
                                                                                    {
                                                                                        Kind = ResourceKind.Modpack
                                                                                    });

                            return new InstanceReferenceModel(profile.Setup.Source,
                                                              r.Label,
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
                foreach (var model in _stageSource.Items)
                {
                    model.IsLocked = false;
                }
            }
        }
    }


    private async Task RefreshPackagesAsync(IReadOnlyList<InstancePackageModel> packages, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        IsRefreshing = true;
        try
        {
            var purls = packages
                       .Select(x => PackageHelper.TryParse(x.Entry.Purl, out var purl)
                                        ? (Model: x, Purl: purl)
                                        : throw new FormatException($"Failed to parse purl: {x.Entry.Purl}"))
                       .ToDictionary(x => x.Purl, x => new RefreshIntermediateData(x.Model));
            var knownVids = purls.Where(x => x.Key.Vid is not null).ToList();
            var unknownVids = purls.Where(x => x.Key.Vid is null).ToList();

            if (token.IsCancellationRequested)
            {
                return;
            }

            // 固定 Vid 的不需要 Filter
            var knownPackages = await dataService.ResolvePackagesAsync(knownVids.Select(x => x.Key), Filter.None);
            if (token.IsCancellationRequested)
            {
                return;
            }

            var unknownProjects =
                await dataService.QueryProjectsAsync(unknownVids.Select(x => (x.Key.Label, x.Key.Namespace,
                                                                              x.Key.Pid)));
            if (token.IsCancellationRequested)
            {
                return;
            }

            var thumbnailsTasks = knownPackages
                                 .Select(async x => (Purl: (x.Label, x.Namespace, x.ProjectId, (string?)x.VersionId),
                                                     Thumbnail: x.Thumbnail is not null
                                                                    ? await dataService.GetBitmapAsync(x.Thumbnail)
                                                                    : AssetUriIndex.DirtImageBitmap))
                                 .Concat(unknownProjects.Select(async x =>
                                                                    (Purl: (x.Label, x.Namespace, x.ProjectId,
                                                                            (string?)null),
                                                                     Thumbnail: x.Thumbnail is not null
                                                                         ? await dataService
                                                                              .GetBitmapAsync(x.Thumbnail)
                                                                         : AssetUriIndex.DirtImageBitmap)))
                                 .ToList();
            await Task.WhenAll(thumbnailsTasks);
            if (token.IsCancellationRequested)
            {
                return;
            }

            foreach (var package in knownPackages)
            {
                purls[(package.Label, package.Namespace, package.ProjectId, package.VersionId)].Package = package;
            }

            foreach (var project in unknownProjects)
            {
                purls[(project.Label, project.Namespace, project.ProjectId, null)].Project = project;
            }

            foreach (var thumbnailsTask in thumbnailsTasks)
            {
                purls[thumbnailsTask.Result.Purl].Thumbnail = thumbnailsTask.Result.Thumbnail;
            }

            foreach (var x in purls.Values)
            {
                InstancePackageInfoModel info = x switch
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
                    _ => throw new UnreachableException()
                };

                x.Model.Info = info;
                x.Model.OldPurlCache = x.Model.Entry.Purl;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                notificationService.PopMessage(ex.Message, "Failed to parse purl", GrowlLevel.Danger);
            });
        }

        IsRefreshing = false;
    }

    #endregion

    #region Fields

    private CancellationToken? _lifetimeToken;
    private CancellationTokenSource? _pageCancellationTokenSource;
    private readonly SourceCache<InstancePackageModel, Profile.Rice.Entry> _stageSource = new(x => x.Entry);
    private IDisposable? _updatingSubscription;
    private readonly CompositeDisposable _subscriptions = new();

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

    protected override Task OnInitializeAsync()
    {
        _lifetimeToken = PageToken;
        _pageCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(PageToken);


        _stageSource
           .Connect()
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
        _stageSource
           .Connect()
           .Filter(enability)
           .Filter(lockility)
           .Filter(kind)
           .Filter(tags)
           .Filter(text)
           .Bind(out var view)
           .Subscribe()
           .DisposeWith(_subscriptions);
        StageView = view;

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

    #region State

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
            //  实际是状态订阅有三层，第一层由 InstanceViewModelBase 维护，且正确工作
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
                               .ProgressStream.Buffer(TimeSpan.FromSeconds(1))
                               .Where(x => x.Any())
                               .Select(x => x.Last())
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
            bool it => x.IsLocked == it,
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
                        persistenceService.AppendAction(new(Basic.Key,
                                                            PersistenceService.ActionKind.EditLoader,
                                                            old,
                                                            lurl));
                    }
                }
                else
                {
                    var old = guard.Value.Setup.Loader;
                    guard.Value.Setup.Loader = null;
                    if (old != null)
                    {
                        persistenceService.AppendAction(new(Basic.Key,
                                                            PersistenceService.ActionKind.EditLoader,
                                                            old,
                                                            null));
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
            overlayService.PopModal(new ProfileRulesModal { Rules = Rules, Packages = _stageSource.Items, OverlayService = overlayService });
        }
    }

    private bool CanViewPackage(InstancePackageInfoModel? model) => model is not null;

    [RelayCommand(CanExecute = nameof(CanViewPackage))]
    private void ViewPackage(InstancePackageInfoModel? model)
    {
        if (model is not null && ProfileManager.TryGetMutable(Basic.Key, out var guard))
        {
            overlayService.PopModal(new InstancePackageModal
            {
                DataContext = model,
                Guard = guard,
                DataService = dataService,
                OverlayService = overlayService,
                PersistenceService = persistenceService,
                Collection = _stageSource,
                Filter = new(Kind: model.Kind,
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
                var project = await dataService.QueryProjectAsync(source.Label, source.Namespace, source.Pid);
                var model = new ExhibitModpackModel(project.Label,
                                                    project.Namespace,
                                                    project.ProjectId,
                                                    project.ProjectName,
                                                    project.Author,
                                                    project.Reference,
                                                    project.Tags,
                                                    project.DownloadCount,
                                                    project.Summary,
                                                    project.UpdatedAt,
                                                    [.. project.Gallery.Select(x => x.Url)]);
                overlayService.PopToast(new ExhibitModpackToast
                {
                    DataService = dataService,
                    DataContext = model,
                    InstallCommand = InstallVersionCommand
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to load project information", GrowlLevel.Warning);
            }
        }
    }

    [RelayCommand]
    private void GotoPackageExplorerView() => navigationService.Navigate<PackageExplorerView>(Basic.Key);


    [RelayCommand]
    private async Task UpdateBatchAsync()
    {
        if (ProfileManager.TryGetImmutable(Basic.Key, out var profile))
        {
            var total = _stageSource.Items.Count;
            var notification = new GrowlItem
            {
                Title = Resources.InstanceSetupView_PackageBulkUpdatingProgressingNotificationTitle,
                Content = Resources
                         .InstanceSetupView_PackageBulkUpdatingProgressingNotificationMessage.Replace("{0}", "0")
                         .Replace("{1}", _stageSource.Count.ToString()),
                IsProgressBarVisible = true,
                IsCloseButtonVisible = false
            };
            notification.Actions.Add(new(Resources
                                            .InstanceSetupView_PackageBulkUpdatingProgressingNotificationCancelText,
                                         new RelayCommand(Cancel)));

            notificationService.Pop(notification);

            var filter = new Filter(Kind: null,
                                    Version: profile.Setup.Version,
                                    Loader: profile.Setup.Loader is not null
                                                ? LoaderHelper.TryParse(profile.Setup.Loader, out var loader)
                                                      ? loader.Identity
                                                      : null
                                                : null);

            var updates = new ConcurrentBag<PackageUpdaterModel>();
            try
            {
                // 值设置太大会触发 API 限制
                var semaphore = new SemaphoreSlim(2);
                // 这里无法使用批量查询来优化，ResolveBatch 无版本限制会 Fallback 到获取所有版本并筛选合适的，这个无法避免
                // ReSharper disable once AccessToDisposedClosure
                var tasks = _stageSource.Items.Select(x => UpdateAsync(x, semaphore, notification.Token));
                await Task.WhenAll(tasks);
                semaphore.Dispose();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to load project information", GrowlLevel.Warning);
            }

            if (notification.Token.IsCancellationRequested)
            {
                return;
            }

            notification.Dismiss();
            var reviewNotification = new GrowlItem
            {
                Title = Resources.InstanceSetupView_PackageBulkUpdatingProgressedNotificationTitle,
                Content = Resources
                         .InstanceSetupView_PackageBulkUpdatingProgressedNotificationMessage
                         .Replace("{0}", updates.Count.ToString())
            };
            reviewNotification.Actions.Add(new(Resources
                                                  .InstanceSetupView_PackageBulkUpdatingProgressedNotificationReviewText,
                                               new AsyncRelayCommand(ReviewAsync, CanReview)));
            notificationService.Pop(reviewNotification);
            return;


            async Task UpdateAsync(InstancePackageModel entry, SemaphoreSlim semaphore, CancellationToken token)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await semaphore.WaitAsync(token);
                if (!entry.IsLocked && PackageHelper.TryParse(entry.Entry.Purl, out var result))
                {
                    if (result.Vid is not null)
                    {
                        try
                        {
                            var resolved = await dataService
                                                .ResolvePackageAsync(result.Label,
                                                                     result.Namespace,
                                                                     result.Pid,
                                                                     null,
                                                                     filter,
                                                                     false)
                                                .ConfigureAwait(false);
                            if (resolved.VersionId != result.Vid)
                            {
                                var package = await dataService
                                                   .ResolvePackageAsync(result.Label,
                                                                        result.Namespace,
                                                                        result.Pid,
                                                                        result.Vid,
                                                                        Filter.None)
                                                   .ConfigureAwait(false);
                                var model = new PackageUpdaterModel(entry,
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
                            notificationService.PopMessage(ex, entry.Entry.Purl, GrowlLevel.Warning);
                        }
                    }
                }

                Interlocked.Decrement(ref total);
                semaphore.Release();

                Dispatcher.UIThread.Post(() =>
                {
                    notification.Progress = Math.Min(100d,
                                                     100d
                                                   * (_stageSource.Items.Count - total)
                                                   / _stageSource.Items.Count);
                    notification.Content = Resources
                                          .InstanceSetupView_PackageBulkUpdatingProgressingNotificationMessage
                                          .Replace("{0}", updates.Count.ToString())
                                          .Replace("{1}", total.ToString());
                });
            }

            void Cancel() => notification.Dismiss();

            bool CanReview() => !updates.IsEmpty;

            async Task ReviewAsync()
            {
                // 这里有个性能 Trick，使用的是不可变 Profile 引用，这里不使用 ProfileGuard 是为了避免重复刷新
                // 由于 Profile 是单例的，实际改变已经被应用，只是没有用 guard.DisposeAsync 通知写入硬盘
                // 为什么不用 Guard：
                // 除了避免触发无效的刷新 diff 以外还有一个原因是这里涉及跨三个控制流且可以在任意一层中断
                // 导致 Guard 无法保证能被释放而出现泄露
                // 缺陷：可能会导致批量更新未能保存到硬盘，例如进程被杀的情况
                var dialog = new PackageBulkUpdaterDialog { Result = updates.ToList() };
                reviewNotification.Dismiss();
                if (await overlayService.PopDialogAsync(dialog)
                 && dialog.Result is IReadOnlyList<PackageUpdaterModel> results)
                {
                    foreach (var model in results.Where(x => x.IsChecked))
                    {
                        var old = model.Model.Entry.Purl;
                        model.Model.Info?.Version = new InstancePackageVersionModel(model.NewVersionId,
                            model.NewVersionName,
                            string.Join(",",
                                        model.Package.Requirements.AnyOfLoaders
                                             .Select(LoaderHelper.ToDisplayName)),
                            string.Join(",", model.Package.Requirements.AnyOfVersions),
                            model.NewVersionTimeRaw,
                            model.Package.ReleaseType,
                            model.Package.Dependencies);
                        // 设置 Version 会同步到 Entry.Purl
                        persistenceService.AppendAction(new(Basic.Key,
                                                            PersistenceService.ActionKind.EditPackage,
                                                            old,
                                                            model.Model.Entry.Purl));
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
                    notificationService.PopMessage("No packages found in the file",
                                                   "Import package list",
                                                   GrowlLevel.Warning);
                    return;
                }

                var addedCount = 0;
                var updatedCount = 0;
                var failedCount = 0;

                if (ProfileManager.TryGetMutable(Basic.Key, out var guard))
                {
                    await using (guard)
                    {
                        foreach (var importedEntry in importedEntries)
                        {
                            try
                            {
                                if (string.IsNullOrEmpty(importedEntry.Purl))
                                {
                                    failedCount++;
                                    continue;
                                }

                                // 查找是否已存在相同的包（基于 Label, Namespace, ProjectId）
                                Profile.Rice.Entry? existingEntry = null;
                                if (PackageHelper.TryParse(importedEntry.Purl, out var importedPurl))
                                {
                                    existingEntry =
                                        guard.Value.Setup.Packages.FirstOrDefault(x => PackageHelper.IsMatched(x.Purl,
                                            importedPurl.Label,
                                            importedPurl.Namespace,
                                            importedPurl.Pid));
                                }

                                if (existingEntry != null)
                                {
                                    // 更新现有包的版本
                                    var oldPurl = existingEntry.Purl;
                                    existingEntry.Purl = importedEntry.Purl;
                                    existingEntry.Enabled = importedEntry.Enabled;

                                    persistenceService.AppendAction(new(Basic.Key,
                                                                        PersistenceService.ActionKind.EditPackage,
                                                                        oldPurl,
                                                                        importedEntry.Purl));
                                    updatedCount++;
                                }
                                else
                                {
                                    // 添加新包
                                    var newEntry = new Profile.Rice.Entry(importedEntry.Purl,
                                                                          importedEntry.Enabled,
                                                                          importedEntry.Source,
                                                                          []);
                                    guard.Value.Setup.Packages.Add(newEntry);
                                    persistenceService.AppendAction(new(Basic.Key,
                                                                        PersistenceService.ActionKind.EditPackage,
                                                                        null,
                                                                        importedEntry.Purl));
                                    addedCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Failed to import package: {purl}", importedEntry.Purl);
                                failedCount++;
                            }
                        }
                    }
                }

                // 显示结果通知
                var resultMessage = $"Added: {addedCount}, Updated: {updatedCount}, Failed: {failedCount}";
                var level = failedCount > 0 ? GrowlLevel.Warning : GrowlLevel.Success;
                notificationService.PopMessage(resultMessage, "Import package list completed", level);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to import package list from file: {path}", filePath);
                notificationService.PopMessage(ex, "Import package list");
            }
        }
    }

    [RelayCommand]
    private async Task ExportListAsync()
    {
        var profile = ProfileManager.GetImmutable(Basic.Key);
        var list = new List<Profile.Rice.Entry>(profile.Setup.Packages);
        var dialog = new PackageListExporterDialog { PackageCount = list.Count };
        if (await overlayService.PopDialogAsync(dialog) && dialog.Result is string path)
        {
            var notification = new GrowlItem { Title = "Export package list to file", IsProgressBarVisible = true };
            var output = new List<ExportedEntry>();
            notification.ProgressMaximum = list.Count;
            notificationService.Pop(notification);
            // 这里用单个解析也没关系，能进入这个页面就说明所有数据都被缓存过了
            foreach (var entry in list)
            {
                if (notification.Token.IsCancellationRequested)
                {
                    return;
                }

                string? label = null;
                string? @namespace = null;
                string? projectId = null;
                string? versionId = null;
                string? name = null;
                string? version = null;
                if (PackageHelper.TryParse(entry.Purl, out var result))
                {
                    label = result.Label;
                    @namespace = result.Namespace;
                    projectId = result.Pid;
                    versionId = result.Vid;
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(25));
                        var package = await dataService.ResolvePackageAsync(result.Label,
                                                                            result.Namespace,
                                                                            result.Pid,
                                                                            result.Vid,
                                                                            Filter.None);
                        name = package.ProjectName;
                        version = package.VersionName;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to exporting: {}", entry.Purl);
                        notificationService.PopMessage($"{entry.Purl}: {ex.Message}",
                                                       "Failed to fetching information",
                                                       GrowlLevel.Warning);
                    }
                }

                output.Add(new(entry.Purl,
                               label,
                               @namespace,
                               projectId,
                               versionId,
                               entry.Enabled,
                               entry.Source,
                               name,
                               version));
                notification.Progress = output.Count;
                notification.Content = $"Exporting package list...({output.Count}/{list.Count})";
            }

            notification.Dismiss();

            try
            {
                var dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                await using (var writer = new StreamWriter(path))
                await using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    await csv.WriteRecordsAsync(output);
                }

                notificationService.PopMessage($"Exported package list to file {path}",
                                               "Export package list to file",
                                               GrowlLevel.Success);
            }
            catch (Exception ex)
            {
                notificationService.PopMessage($"Writing data to file ({path}) failed: {ex.Message}",
                                               "Export package list to file",
                                               GrowlLevel.Danger);
            }
        }
    }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        if (Reference is { Value: InstanceReferenceModel reference }
         && PackageHelper.TryParse(reference.Purl, out var result))
        {
            try
            {
                var page = await dataService.InspectVersionsAsync(result.Label,
                                                                  result.Namespace,
                                                                  result.Pid,
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
                logger.LogError(ex, "Failed to check update: {}", reference.Purl);
                notificationService.PopMessage(ex, "Failed to check update");
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
            notificationService.PopMessage(ex, "Update failed");
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
            notificationService.PopMessage($"{version.ProjectName}({version.VersionName}) has added to install queue");
        }
    }

    [RelayCommand]
    private async Task RemovePackage(InstancePackageModel? model)
    {
        if (model is not null && ProfileManager.TryGetMutable(Basic.Key, out var guard))
        {
            await using (guard)
            {
                guard.Value.Setup.Packages.Remove(model.Entry);
                persistenceService.AppendAction(new(Basic.Key,
                                                    PersistenceService.ActionKind.EditPackage,
                                                    model.Entry.Purl,
                                                    null));
            }
        }
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial int LayoutIndex { get; set; } = configurationService.Value.InterfaceSetupLayout;

    partial void OnLayoutIndexChanged(int value) => configurationService.Value.InterfaceSetupLayout = value;

    [ObservableProperty]
    public partial LazyObject? Reference { get; set; }

    [ObservableProperty]
    public partial string LoaderLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int StageCount { get; set; }

    [ObservableProperty]
    public partial double UpdatingProgress { get; set; }

    [ObservableProperty]
    public partial bool UpdatingPending { get; set; } = true;

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; } = false;

    [ObservableProperty]
    public partial ReadOnlyObservableCollection<InstancePackageModel>? StageView { get; set; }

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

    #endregion
}
