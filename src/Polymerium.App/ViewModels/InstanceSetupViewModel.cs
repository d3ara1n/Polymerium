using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Dialogs;
using Polymerium.App.Facilities;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Toasts;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.ViewModels;

public partial class InstanceSetupViewModel(
    ViewBag bag,
    ProfileManager profileManager,
    RepositoryAgent repositories,
    NotificationService notificationService,
    InstanceManager instanceManager,
    DataService dataService,
    OverlayService overlayService) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    #region Injected

    private readonly InstanceManager _instanceManager = instanceManager;

    #endregion

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
                    // NOTE: 如果可以的话，对于 Purl.Vid 为空的，需要去 LockData 里找 Packages[i].Vid，获取锁定的版本
                    //  当然也可以不这么干，对于版本锁，给一个单独的页面来查看锁定的版本就行
                    //  因为版本锁是为构建服务的，不应该暴露给用户看当前锁定的版本是哪个
                    var p = await dataService
                                 .ResolvePackageAsync(v.Label, v.Namespace, v.Pid, v.Vid, Filter.Empty)
                                 .ConfigureAwait(false);
                    Dispatcher.UIThread.Post(() => RefreshingCount++);
                    return new InstancePackageModel(entry,
                                                    entry.Source == Basic.Source,
                                                    p.Label,
                                                    p.ProjectName,
                                                    p.VersionName,
                                                    p.Summary,
                                                    p.Reference,
                                                    p.Thumbnail is not null
                                                        ? await dataService
                                                               .GetBitmapAsync(p.Thumbnail)
                                                               .ConfigureAwait(false)
                                                        : AssetUriIndex.DIRT_IMAGE_BITMAP,
                                                    p.Kind);
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
            LoaderLabel = "None";

        _updatingSubscription?.Dispose();
        UpdatingPending = true;
        UpdatingProgress = 0;
    }

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        _pageCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (!InstanceManager.IsTracking(Basic.Key, out var tracker) || tracker is not UpdateTracker)
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
    private void OpenSourceUrl(Uri? url)
    {
        if (url is not null)
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchUriAsync(url);
    }

    [RelayCommand]
    private void ViewPackage(InstancePackageModel? model)
    {
        if (model is not null)
            overlayService.PopModal(new PackageEntryModal { Model = model });
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
                                                    project.ProjectId,
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
    private async Task CheckUpdate()
    {
        if (Reference is { Value: InstanceReferenceModel reference }
         && PackageHelper.TryParse(reference.Purl, out var result))
        {
            var page = await (await repositories.InspectAsync(result.Label,
                                                              result.Namespace,
                                                              result.Pid,
                                                              Filter.Empty with { Kind = ResourceKind.Modpack }))
                          .FetchAsync();
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
            var dialog = new InstanceVersionPickerDialog { Versions = versions };
            if (await overlayService.PopDialogAsync(dialog) && dialog.Result is InstanceReferenceVersionModel version)
                Update(version);
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
            _instanceManager.Install(version.ProjectName,
                                     version.Label,
                                     version.Namespace,
                                     version.ProjectId,
                                     version.Versionid);
            notificationService.PopMessage($"{version.ProjectName}({version.VersionName}) has added to install queue");
        }
    }

    #endregion

    #region Reactive

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