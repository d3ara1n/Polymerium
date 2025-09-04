using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CsvHelper;
using DynamicData;
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
using Trident.Core.Services;
using Trident.Core.Services.Instances;
using Refit;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
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
    #region Other

    private void TriggerRefresh(CancellationToken token)
    {
        _refreshingCancellationTokenSource?.Cancel();
        _refreshingCancellationTokenSource?.Dispose();
        _refreshingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        var inner = _refreshingCancellationTokenSource.Token;
        if (ProfileManager.TryGetImmutable(Basic.Key, out var profile))
        {
            Stage.Clear();
            StageCount = profile.Setup.Packages.Count;
            IsRefreshing = true;
            Task.Run(async () =>
                     {
                         // 有 vid 的走 ResolvePackagesAsync 没有的走 QueryProjectAsync
                         try
                         {
                             var purls = profile
                                        .Setup.Packages
                                        .Select(x => PackageHelper.TryParse(x.Purl, out var purl)
                                                         ? (Entry: x, Purl: purl)
                                                         : throw new FormatException($"Failed to parse purl: {x.Purl}"))
                                        .ToDictionary(x => x.Purl, x => new RefreshIntermediateData(x.Entry));
                             var knownVids = purls.Where(x => x.Key.Vid is not null).ToList();
                             var unknownVids = purls.Where(x => x.Key.Vid is null).ToList();

                             if (inner.IsCancellationRequested)
                                 return;
                             // 固定 Vid 的不需要 Filter
                             var knownPackages = await dataService
                                                      .ResolvePackagesAsync(knownVids.Select(x => x.Key), Filter.None)
                                                      .ConfigureAwait(false);
                             if (inner.IsCancellationRequested)
                                 return;
                             var unknownProjects = await dataService
                                                        .QueryProjectsAsync(unknownVids.Select(x => (x.Key.Label,
                                                                                            x.Key.Namespace,
                                                                                            x.Key.Pid)))
                                                        .ConfigureAwait(false);
                             if (inner.IsCancellationRequested)
                                 return;
                             var thumbnailsTasks = knownPackages
                                                  .Select(async x =>
                                                              (Purl: (x.Label, x.Namespace, x.ProjectId,
                                                                      (string?)x.VersionId),
                                                               Thumbnail: x.Thumbnail is not null
                                                                              ? await dataService
                                                                                 .GetBitmapAsync(x.Thumbnail)
                                                                                 .ConfigureAwait(false)
                                                                              : AssetUriIndex.DirtImageBitmap))
                                                  .Concat(unknownProjects.Select(async x =>
                                                              (Purl: (x.Label, x.Namespace, x.ProjectId,
                                                                      (string?)null),
                                                               Thumbnail: x.Thumbnail is not null
                                                                              ? await dataService
                                                                                 .GetBitmapAsync(x.Thumbnail)
                                                                                 .ConfigureAwait(false)
                                                                              : AssetUriIndex.DirtImageBitmap)))
                                                  .ToList();
                             await Task.WhenAll(thumbnailsTasks).ConfigureAwait(false);
                             if (inner.IsCancellationRequested)
                                 return;
                             foreach (var package in knownPackages)
                             {
                                 purls[(package.Label, package.Namespace, package.ProjectId, package.VersionId)]
                                        .Package =
                                     package;
                             }

                             foreach (var project in unknownProjects)
                             {
                                 purls[(project.Label, project.Namespace, project.ProjectId, null)].Project = project;
                             }

                             foreach (var thumbnailsTask in thumbnailsTasks)
                             {
                                 purls[thumbnailsTask.Result.Purl].Thumbnail = thumbnailsTask.Result.Thumbnail;
                             }

                             var stages = purls
                                         .Select(x => x.Value switch
                                          {
                                              { Package: not null, Thumbnail: not null } => new(x.Value.Entry,
                                                  x.Value.Entry.Source is not null
                                               && x.Value.Entry.Source == Basic.Source,
                                                  x.Value.Package.Label,
                                                  x.Value.Package.Namespace,
                                                  x.Value.Package.ProjectId,
                                                  x.Value.Package.ProjectName,
                                                  new
                                                      InstancePackageVersionModel(x.Value
                                                             .Package.VersionId,
                                                          x.Value.Package.VersionName,
                                                          string.Join(",",
                                                                      x.Value.Package
                                                                       .Requirements
                                                                       .AnyOfLoaders
                                                                       .Select(LoaderHelper
                                                                                  .ToDisplayName)),
                                                          string.Join(",",
                                                                      x.Value.Package
                                                                       .Requirements
                                                                       .AnyOfVersions),
                                                          x.Value.Package.PublishedAt,
                                                          x.Value.Package.ReleaseType,
                                                          x.Value.Package.Dependencies)
                                                      {
                                                          IsCurrent = true
                                                      },
                                                  x.Value.Package.Author,
                                                  x.Value.Package.Summary,
                                                  x.Value.Package.Reference,
                                                  x.Value.Thumbnail,
                                                  x.Value.Package.Kind),
                                              { Project: not null, Thumbnail: not null } => new
                                                  InstancePackageModel(x.Value.Entry,
                                                                       x.Value.Entry.Source is not null
                                                                    && x.Value.Entry.Source == Basic.Source,
                                                                       x.Value.Project.Label,
                                                                       x.Value.Project.Namespace,
                                                                       x.Value.Project.ProjectId,
                                                                       x.Value.Project.ProjectName,
                                                                       InstancePackageUnspecifiedVersionModel.Instance,
                                                                       x.Value.Project.Author,
                                                                       x.Value.Project.Summary,
                                                                       x.Value.Project.Reference,
                                                                       x.Value.Thumbnail,
                                                                       x.Value.Project.Kind),
                                              _ => throw new UnreachableException()
                                          })
                                         .ToList();

                             Dispatcher.UIThread.Post(() =>
                             {
                                 StageCount = stages.Count;
                                 Stage.AddOrUpdate(stages);
                                 IsRefreshing = false;
                             });
                         }
                         catch (OperationCanceledException) { }
                         catch (Exception ex)
                         {
                             Dispatcher.UIThread.Post(() =>
                             {
                                 notificationService.PopMessage(ex.Message,
                                                                "Failed to parse purl",
                                                                NotificationLevel.Danger);
                             });
                         }
                     },
                     inner);
        }

        if (Basic.Source is not null && PackageHelper.TryParse(Basic.Source, out var r))
        {
            Reference = new(async t =>
            {
                var package = await dataService
                                   .ResolvePackageAsync(r.Label,
                                                        r.Namespace,
                                                        r.Pid,
                                                        r.Vid,
                                                        Filter.None with { Kind = ResourceKind.Modpack })
                                   .ConfigureAwait(false);

                return new InstanceReferenceModel(Basic.Source,
                                                  r.Label,
                                                  package.ProjectName,
                                                  package.VersionId,
                                                  package.VersionName,
                                                  package.Thumbnail,
                                                  package.Reference);
            });
        }
    }

    #endregion

    #region Fields

    private CancellationTokenSource? _pageCancellationTokenSource;
    private CancellationTokenSource? _refreshingCancellationTokenSource;
    private IDisposable? _updatingSubscription;

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

        _updatingSubscription?.Dispose();
        UpdatingPending = true;
        UpdatingProgress = 0;
    }

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        _pageCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (!InstanceManager.IsTracking(Basic.Key, out var tracker)
         || tracker is not UpdateTracker and not DeployTracker)
        {
            TriggerRefresh(_pageCancellationTokenSource.Token);
        }

        await base.OnInitializeAsync(token);
    }

    protected override Task OnDeinitializeAsync(CancellationToken token)
    {
        _pageCancellationTokenSource?.Cancel();
        _pageCancellationTokenSource?.Dispose();
        _pageCancellationTokenSource = null;
        _updatingSubscription?.Dispose();

        return base.OnDeinitializeAsync(token);
    }

    #endregion

    #region State

    protected override void OnInstanceUpdating(UpdateTracker tracker)
    {
        _refreshingCancellationTokenSource?.Cancel();
        TrackUpdateProgress(tracker);
        base.OnInstanceUpdating(tracker);
    }

    protected override void OnInstanceUpdated(UpdateTracker tracker)
    {
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

        TriggerRefresh(_pageCancellationTokenSource.Token);
        base.OnInstanceUpdated(tracker);
    }

    protected override void OnInstanceDeployed(DeployTracker tracker)
    {
        if (_pageCancellationTokenSource is null || _pageCancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        TriggerRefresh(_pageCancellationTokenSource.Token);
        base.OnInstanceDeployed(tracker);
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
    private void ViewPackage(InstancePackageModel? model)
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
                StageCollection = Stage,
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
                notificationService.PopMessage(ex, "Failed to load project information", NotificationLevel.Warning);
            }
        }
    }

    [RelayCommand]
    private void GotoPackageExplorerView() => navigationService.Navigate<PackageExplorerView>(Basic.Key);


    [RelayCommand]
    private async Task UpdateBatchAsync(SourceCache<InstancePackageModel, Profile.Rice.Entry>? packages)
    {
        var cts = new CancellationTokenSource();
        if (packages != null && ProfileManager.TryGetMutable(Basic.Key, out var guard))
        {
            var total = packages.Items.Count;
            var notification = new NotificationItem
            {
                Title = Resources.InstanceSetupView_PackageBulkUpdatingProgressingNotificationTitle,
                Content = Resources
                         .InstanceSetupView_PackageBulkUpdatingProgressingNotificationPrompt.Replace("{0}", "0")
                         .Replace("{1}", packages.Count.ToString()),
                IsProgressBarVisible = true,
                IsCloseButtonVisible = false
            };
            notification.Actions.Add(new(Resources
                                            .InstanceSetupView_PackageBulkUpdatingProgressingNotificationCancelText,
                                         new RelayCommand(Cancel)));

            notificationService.Pop(notification);

            var filter = new Filter(Kind: null,
                                    Version: guard.Value.Setup.Version,
                                    Loader: guard.Value.Setup.Loader is not null
                                                ? LoaderHelper.TryParse(guard.Value.Setup.Loader, out var loader)
                                                      ? loader.Identity
                                                      : null
                                                : null);

            var updates = new ConcurrentBag<PackageUpdaterModel>();
            try
            {
                // 值设置太大会触发 API 限制
                var semaphore = new SemaphoreSlim(2);
                var tasks = packages.Items.Select(x => UpdateAsync(x, semaphore, cts.Token));
                await Task.WhenAll(tasks);
                semaphore.Dispose();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to load project information", NotificationLevel.Warning);
            }

            if (cts.IsCancellationRequested)
            {
                return;
            }

            notification.Close();
            var reviewNotification = new NotificationItem
            {
                Title = Resources.InstanceSetupView_PackageBulkUpdatingProgressedNotificationTitle,
                Content = Resources
                         .InstanceSetupView_PackageBulkUpdatingProgressedNotificationPrompt
                         .Replace("{0}", updates.Count.ToString())
            };
            reviewNotification.Actions.Add(new(Resources
                                                  .InstanceSetupView_PackageBulkUpdatingProgressedNotificationReviewText,
                                               new RelayCommand(Review, CanReview)));
            notificationService.Pop(reviewNotification);

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
                            notificationService.PopMessage(ex, entry.Entry.Purl, NotificationLevel.Warning);
                        }
                    }
                }

                Interlocked.Decrement(ref total);
                semaphore.Release();

                Dispatcher.UIThread.Post(() =>
                {
                    notification.Progress = Math.Min(100d,
                                                     100d * (packages.Items.Count - total) / packages.Items.Count);
                    notification.Content = Resources
                                          .InstanceSetupView_PackageBulkUpdatingProgressingNotificationPrompt
                                          .Replace("{0}", updates.Count.ToString())
                                          .Replace("{1}", total.ToString());
                });
            }

            void Cancel()
            {
                cts.Cancel();
                notification.Close();
            }

            bool CanReview() => !updates.IsEmpty;

            void Review()
            {
                var modal = new PackageBulkUpdaterModal
                {
                    DataService = dataService,
                    NotificationService = notificationService,
                    PersistenceService = persistenceService
                };
                modal.SetGuard(guard, updates.ToList());
                overlayService.PopModal(modal);
                reviewNotification.Close();
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
            var notification = new NotificationItem
            {
                Title = "Export package list to file", IsProgressBarVisible = true
            };
            var output = new List<ExportedEntry>();
            notification.ProgressMaximum = list.Count;
            notificationService.Pop(notification);
            foreach (var entry in list)
            {
                string? name = null;
                string? version = null;
                if (PackageHelper.TryParse(entry.Purl, out var result))
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(50));
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
                                                       NotificationLevel.Warning);
                    }
                }

                output.Add(new(entry.Purl, entry.Enabled, entry.Source, [.. entry.Tags], name, version));
                notification.Progress = output.Count;
                notification.Content = $"Exporting package list...({output.Count}/{list.Count})";
            }

            notification.Close();

            try
            {
                var dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                await using (var writer = new StreamWriter(path))
                await using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    await csv.WriteRecordsAsync(output);
                }

                notificationService.PopMessage($"Exported package list to file {path}",
                                               "Export package list to file",
                                               NotificationLevel.Success);
            }
            catch (Exception ex)
            {
                notificationService.PopMessage($"Writing data to file ({path}) failed: {ex.Message}",
                                               "Export package list to file",
                                               NotificationLevel.Danger);
            }
        }
    }

    private record ExportedEntry(
        string Purl,
        bool Enabled,
        string? Source,
        string[] Tags,
        string? Name,
        string? Version);

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
            _refreshingCancellationTokenSource?.Cancel();
            _refreshingCancellationTokenSource?.Dispose();
            _refreshingCancellationTokenSource = null;
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
            guard.Value.Setup.Packages.Remove(model.Entry);
            Stage.Remove(model);
            StageCount--;
            await guard.DisposeAsync();
            persistenceService.AppendAction(new(Basic.Key,
                                                PersistenceService.ActionKind.EditPackage,
                                                model.Entry.Purl,
                                                null));
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
    public partial SourceCache<InstancePackageModel, Profile.Rice.Entry> Stage { get; set; } = new(x => x.Entry);

    [ObservableProperty]
    public partial int StageCount { get; set; }

    [ObservableProperty]
    public partial double UpdatingProgress { get; set; }

    [ObservableProperty]
    public partial bool UpdatingPending { get; set; } = true;

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; } = false;

    #endregion

    #region Nested Type: RefreshIntermediateData

    private class RefreshIntermediateData(Profile.Rice.Entry entry)
    {
        public Profile.Rice.Entry Entry => entry;
        public Bitmap? Thumbnail { get; set; }
        public Project? Project { get; set; }
        public Package? Package { get; set; }
    }

    #endregion
}
