using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
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
    private CancellationTokenSource? _pageCancellationTokenSource;
    private CancellationTokenSource? _refreshingCancellationTokenSource;
    private IDisposable? _updatingSubscription;

    private void TriggerRefresh(CancellationToken token)
    {
        _refreshingCancellationTokenSource?.Cancel();
        _refreshingCancellationTokenSource?.Dispose();
        _refreshingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        var inner = _refreshingCancellationTokenSource.Token;
        try
        {
            if (ProfileManager.TryGetImmutable(Basic.Key, out var profile))
            {
                Stage.Clear();
                StageCount = profile.Setup.Packages.Count;
                RefreshingCount = 0;
                IsRefreshing = true;
                Task.Run(() =>
                         {
                             var tasks = profile.Setup.Packages.Select(LoadAsync).ToList();
                             Task.WaitAll(tasks, inner);
                             var stages = tasks.Where(x => x.Result is not null).Select(x => x.Result!).ToList();
                             Dispatcher.UIThread.Post(() =>
                             {
                                 StageCount = stages.Count;
                                 Stage.AddOrUpdate(stages);
                                 IsRefreshing = false;
                             });
                         },
                         inner);
            }
        }
        catch (Exception ex)
        {
            notificationService.PopMessage(ex, "Loading package list failed", NotificationLevel.Warning);
        }

        if (Basic.Source is not null && PackageHelper.TryParse(Basic.Source, out var r))
            Reference = new LazyObject(t => LoadReferenceAsync(Basic.Source, r.Label, r.Namespace, r.Pid, r.Vid, t));
        return;

        async Task<InstancePackageModel?> LoadAsync(Profile.Rice.Entry entry)
        {
            if (PackageHelper.TryParse(entry.Purl, out var v))
                try
                {
                    if (inner.IsCancellationRequested)
                        return null;
                    if (v.Vid is null)
                    {
                        var p = await dataService.QueryProjectAsync(v.Label, v.Namespace, v.Pid);
                        var t = p.Thumbnail is not null
                                    ? await dataService.GetBitmapAsync(p.Thumbnail).ConfigureAwait(false)
                                    : AssetUriIndex.DIRT_IMAGE_BITMAP;
                        Dispatcher.UIThread.Post(() => RefreshingCount++);
                        return new InstancePackageModel(entry,
                                                        entry.Source is not null && entry.Source == Basic.Source,
                                                        p.Label,
                                                        p.Namespace,
                                                        p.ProjectId,
                                                        p.ProjectName,
                                                        InstancePackageUnspecifiedVersionModel.Instance,
                                                        p.Author,
                                                        p.Summary,
                                                        p.Reference,
                                                        t,
                                                        p.Kind);
                    }
                    else
                    {
                        var p = await dataService
                                     .ResolvePackageAsync(v.Label, v.Namespace, v.Pid, v.Vid, Filter.Empty)
                                     .ConfigureAwait(false);

                        var t = p.Thumbnail is not null
                                    ? await dataService.GetBitmapAsync(p.Thumbnail).ConfigureAwait(false)
                                    : AssetUriIndex.DIRT_IMAGE_BITMAP;
                        Dispatcher.UIThread.Post(() => RefreshingCount++);
                        return new InstancePackageModel(entry,
                                                        entry.Source is not null && entry.Source == Basic.Source,
                                                        p.Label,
                                                        p.Namespace,
                                                        p.ProjectId,
                                                        p.ProjectName,
                                                        new InstancePackageVersionModel(p.VersionId,
                                                            p.VersionName,
                                                            string.Join(",",
                                                                        p.Requirements.AnyOfLoaders
                                                                         .Select(LoaderHelper.ToDisplayName)),
                                                            string.Join(",", p.Requirements.AnyOfVersions),
                                                            p.PublishedAt,
                                                            p.ReleaseType,
                                                            p.Dependencies) { IsCurrent = true },
                                                        p.Author,
                                                        p.Summary,
                                                        p.Reference,
                                                        t,
                                                        p.Kind);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    notificationService.PopMessage($"{entry.Purl}: {ex.Message}",
                                                   "Failed to parse purl",
                                                   NotificationLevel.Warning);
                }

            return null;
        }

        async Task<object?> LoadReferenceAsync(
            string purl,
            string label,
            string? @namespace,
            string pid,
            string? vid,
            CancellationToken _)
        {
            try
            {
                var package = await dataService
                                   .ResolvePackageAsync(label,
                                                        @namespace,
                                                        pid,
                                                        vid,
                                                        Filter.Empty with { Kind = ResourceKind.Modpack })
                                   .ConfigureAwait(false);

                return new InstanceReferenceModel(purl,
                                                  label,
                                                  package.ProjectName,
                                                  package.VersionId,
                                                  package.VersionName,
                                                  package.Thumbnail,
                                                  package.Reference);
            }
            catch (Exception ex)
            {
                notificationService.PopMessage($"{Basic.Source}: {ex.Message}",
                                               "Fetching modpack information failed",
                                               NotificationLevel.Warning);
                throw;
            }
        }
    }

    protected override void OnUpdateModel(string key, Profile profile)
    {
        base.OnUpdateModel(key, profile);
        if (profile.Setup.Loader is not null && LoaderHelper.TryParse(profile.Setup.Loader, out var result))
            LoaderLabel = LoaderHelper.ToDisplayLabel(result.Identity, result.Version);
        else
            LoaderLabel = Resources.Enum_None;

        _updatingSubscription?.Dispose();
        UpdatingPending = true;
        UpdatingProgress = 0;
    }

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        _pageCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (!InstanceManager.IsTracking(Basic.Key, out var tracker)
         || tracker is not UpdateTracker and not DeployTracker)
            TriggerRefresh(_pageCancellationTokenSource.Token);

        await base.OnInitializedAsync(token);
    }

    protected override Task OnDeinitializeAsync(CancellationToken token)
    {
        _pageCancellationTokenSource?.Cancel();
        _pageCancellationTokenSource?.Dispose();
        _pageCancellationTokenSource = null;
        _updatingSubscription?.Dispose();

        return base.OnDeinitializeAsync(token);
    }

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
            return;

        TriggerRefresh(_pageCancellationTokenSource.Token);
        base.OnInstanceUpdated(tracker);
    }

    protected override void OnInstanceDeployed(DeployTracker tracker)
    {
        if (_pageCancellationTokenSource is null || _pageCancellationTokenSource.IsCancellationRequested)
            return;

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

    #region Commands

    [RelayCommand]
    private async Task EditLoader()
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
            if (ProfileManager.TryGetMutable(Basic.Key, out var guard))
            {
                if (dialog.Result is LoaderCandidateSelectionModel selection)
                    guard.Value.Setup.Loader = LoaderHelper.ToLurl(selection.Id, selection.Version);
                else
                    guard.Value.Setup.Loader = null;
                await guard.DisposeAsync();
            }
    }

    [RelayCommand]
    private void ViewPackage(InstancePackageModel? model)
    {
        if (model is not null && ProfileManager.TryGetMutable(Basic.Key, out var guard))
            overlayService.PopModal(new InstancePackageModal
            {
                DataContext = model,
                Guard = guard,
                DataService = dataService,
                OverlayService = overlayService,
                PersistenceService = persistenceService,
                OriginalCollection = Stage.Items,
                Filter = new Filter(Kind: model.Kind,
                                    Version: Basic.Version,
                                    Loader: Basic.Loader is not null
                                                ? LoaderHelper.TryParse(Basic.Loader,
                                                                        out var result)
                                                      ? result.Identity
                                                      : null
                                                : null)
            });
    }

    [RelayCommand]
    private async Task ViewDetails()
    {
        if (Basic.Source is not null && PackageHelper.TryParse(Basic.Source, out var source))
            try
            {
                var versions = await dataService.InspectVersionsAsync(source.Label,
                                                                      source.Namespace,
                                                                      source.Pid,
                                                                      Filter.Empty with
                                                                      {
                                                                          Kind = ResourceKind.Modpack
                                                                      });
                var project = await dataService.QueryProjectAsync(source.Label, source.Namespace, source.Pid);
                var model = new ExhibitModpackModel(project.ProjectName,
                                                    project.Author,
                                                    project.Label,
                                                    project.Reference,
                                                    project.Tags,
                                                    project.DownloadCount,
                                                    project.Summary,
                                                    string.Empty,
                                                    project.UpdatedAt,
                                                    project.Gallery.Select(x => x.Url).ToList(),
                                                    versions
                                                       .Select(x => new ExhibitVersionModel(project.Label,
                                                                   project.Namespace,
                                                                   project.ProjectName,
                                                                   project.ProjectId,
                                                                   x.VersionName,
                                                                   x.VersionId,
                                                                   string.Join(",",
                                                                               x.Requirements.AnyOfLoaders
                                                                                  .Select(LoaderHelper
                                                                                      .ToDisplayName)),
                                                                   string.Join(",", x.Requirements.AnyOfVersions),
                                                                   string.Empty,
                                                                   x.PublishedAt,
                                                                   x.DownloadCount,
                                                                   x.ReleaseType,
                                                                   PackageHelper.ToPurl(x.Label,
                                                                       x.Namespace,
                                                                       x.ProjectId,
                                                                       x.VersionId)))
                                                       .ToList());
                overlayService.PopToast(new ExhibitModpackToast
                {
                    DataContext = model, InstallCommand = InstallVersionCommand
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to load project information", NotificationLevel.Warning);
            }
    }

    [RelayCommand]
    private void GotoPackageExplorerView()
    {
        navigationService.Navigate<PackageExplorerView>(Basic.Key);
    }


    [RelayCommand]
    private async Task UpdateBatchAsync(SourceCache<InstancePackageModel, Profile.Rice.Entry>? packages)
    {
        var cts = new CancellationTokenSource();
        if (packages != null && ProfileManager.TryGetMutable(Basic.Key, out var guard))
        {
            var updates = new List<PackageUpdaterModel>();
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
            notification.Actions.Add(new NotificationAction(Resources
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
            foreach (var entry in packages.Items)
            {
                if (cts.IsCancellationRequested)
                    break;
                if (!entry.IsLocked && PackageHelper.TryParse(entry.Entry.Purl, out var result))
                    if (result.Vid is not null)
                        try
                        {
                            // 可以引入并发哈哈，但得客户提出需求再引入
                            var resolved = await dataService.ResolvePackageAsync(result.Label,
                                               result.Namespace,
                                               result.Pid,
                                               null,
                                               filter,
                                               false);
                            if (resolved.VersionId != result.Vid)
                            {
                                var package = await dataService.ResolvePackageAsync(result.Label,
                                                  result.Namespace,
                                                  result.Pid,
                                                  result.Vid,
                                                  Filter.Empty);
                                var model = new PackageUpdaterModel(entry,
                                                                    package,
                                                                    package.Thumbnail ?? AssetUriIndex.DIRT_IMAGE,
                                                                    package.VersionId,
                                                                    package.VersionName,
                                                                    package.PublishedAt,
                                                                    resolved.VersionId,
                                                                    resolved.VersionName,
                                                                    resolved.PublishedAt);
                                updates.Add(model);
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            notificationService.PopMessage(ex, entry.Entry.Purl, NotificationLevel.Warning);
                        }

                total--;

                notification.Progress = Math.Min(100d, 100d * updates.Count / total);
                notification.Content = Resources
                                      .InstanceSetupView_PackageBulkUpdatingProgressingNotificationPrompt
                                      .Replace("{0}", updates.Count.ToString())
                                      .Replace("{1}", total.ToString());
            }

            if (cts.IsCancellationRequested)
                return;

            notification.Close();
            var reviewNotification = new NotificationItem
            {
                Title = Resources.InstanceSetupView_PackageBulkUpdatingProgressedNotificationTitle,
                Content = Resources
                         .InstanceSetupView_PackageBulkUpdatingProgressedNotificationPrompt
                         .Replace("{0}", updates.Count.ToString())
            };
            reviewNotification.Actions.Add(new NotificationAction(Resources
                                                                     .InstanceSetupView_PackageBulkUpdatingProgressedNotificationReviewText,
                                                                  new RelayCommand(Review, CanReview)));
            notificationService.Pop(reviewNotification);

            void Cancel()
            {
                cts.Cancel();
                notification.Close();
            }

            bool CanReview() => updates.Count > 0;

            void Review()
            {
                var modal = new PackageBulkUpdaterModal
                {
                    DataService = dataService,
                    NotificationService = notificationService,
                    PersistenceService = persistenceService
                };
                modal.SetGuard(guard, updates);
                overlayService.PopModal(modal);
                reviewNotification.Close();
            }
        }
    }

    [RelayCommand]
    private async Task ExportList()
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
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(50));
                        var package = await dataService.ResolvePackageAsync(result.Label,
                                                                            result.Namespace,
                                                                            result.Pid,
                                                                            result.Vid,
                                                                            Filter.Empty);
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

                output.Add(new ExportedEntry(entry.Purl,
                                             entry.Enabled,
                                             entry.Source,
                                             entry.Tags.ToArray(),
                                             name,
                                             version));
                notification.Progress = output.Count;
                notification.Content = $"Exporting package list...({output.Count}/{list.Count})";
            }

            notification.Close();

            try
            {
                var dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
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
            try
            {
                var page = await dataService.InspectVersionsAsync(result.Label,
                                                                  result.Namespace,
                                                                  result.Pid,
                                                                  Filter.Empty with
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
                    Update(version);
            }
            catch (ApiException ex)
            {
                logger.LogError(ex, "Failed to check update: {}", reference.Purl);
                notificationService.PopMessage(ex, "Failed to check update");
            }
    }

    private bool CanUpdate(InstanceReferenceVersionModel? model) => model is { IsCurrent: false };

    [RelayCommand(CanExecute = nameof(CanUpdate))]
    private void Update(InstanceReferenceVersionModel? model)
    {
        if (model is null)
            return;

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
            persistenceService.AppendAction(new PersistenceService.Action(Basic.Key,
                                                                          PersistenceService.ActionKind.EditPackage,
                                                                          model.Entry.Purl,
                                                                          null));
        }
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial int LayoutIndex { get; set; } = configurationService.Value.InterfaceSetupLayout;

    partial void OnLayoutIndexChanged(int value)
    {
        configurationService.Value.InterfaceSetupLayout = value;
    }

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

    [ObservableProperty]
    public partial int RefreshingCount { get; set; }

    #endregion
}