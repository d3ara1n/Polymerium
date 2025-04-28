using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Refit;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.ViewModels;

public partial class PackageExplorerViewModel : ViewModelBase
{
    private readonly CompositeDisposable _subscriptions = new();

    public PackageExplorerViewModel(
        ViewBag bag,
        RepositoryAgent agent,
        DataService dataService,
        ProfileManager profileManager,
        NotificationService notificationService,
        OverlayService overlayService)
    {
        _agent = agent;
        _dataService = dataService;
        _profileManager = profileManager;
        _notificationService = notificationService;
        _overlayService = overlayService;

        if (bag.Parameter is string key)
        {
            if (profileManager.TryGetImmutable(key, out var profile))
                Basic = new InstanceBasicModel(key,
                                               profile.Name,
                                               profile.Setup.Version,
                                               profile.Setup.Loader,
                                               profile.Setup.Source);
            else
                throw new PageNotReachedException(typeof(InstanceView),
                                                  $"Key '{key}' is not valid instance or not found");
        }
        else
        {
            throw new PageNotReachedException(typeof(InstanceView), "Key to the instance is not provided");
        }


        var r = agent.Labels.Select(x => new RepositoryBasicModel(x, x.ToString().ToUpper())).ToList();
        Repositories = r;
        SelectedRepository = r.First();
        IsFilterEnabled = true;
        PendingPackagesSource
           .Connect()
           .Filter(x => x.State == ExhibitState.Adding)
           .Bind(out var adding)
           .Subscribe()
           .DisposeWith(_subscriptions);
        PendingPackagesSource
           .Connect()
           .Filter(x => x.State == ExhibitState.Modifying)
           .Bind(out var modifying)
           .Subscribe()
           .DisposeWith(_subscriptions);
        PendingPackagesSource
           .Connect()
           .Filter(x => x.State == ExhibitState.Removing)
           .Bind(out var removing)
           .Subscribe()
           .DisposeWith(_subscriptions);
        AddingPackagesView = adding;
        ModifyingPackagesView = modifying;
        RemovingPackagesView = removing;
    }

    public IEnumerable<RepositoryBasicModel> Repositories { get; }

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;

        foreach (var repository in Repositories)
            if (repository.Loaders == null || repository.Versions == null)
            {
                var status = await _dataService.CheckStatusAsync(repository.Label);
                repository.Kinds = status.SupportedKinds.Where(x => x != ResourceKind.Modpack).ToList();
            }
    }

    private void ModifyPending(ExhibitModel model)
    {
        if (model.State is null or ExhibitState.Editable)
            PendingPackagesSource.RemoveKey(model);
        else
            PendingPackagesSource.AddOrUpdate(model);
    }

    private ExhibitModel LinkExhibit(Project project)
    {
        var foundInPending = PendingPackagesSource.Items.FirstOrDefault(x => x.Label == project.Label
                                                                          && x.Ns == project.Namespace
                                                                          && x.ProjectId == project.ProjectId);
        if (foundInPending != null)
            return foundInPending;

        var foundInResult = Exhibits?.FirstOrDefault(x => x.Label == project.Label
                                                       && x.Ns == project.Namespace
                                                       && x.ProjectId == project.ProjectId);
        if (foundInResult != null)
            return foundInResult;

        var profile = _profileManager.GetImmutable(Basic.Key);
        var model = new ExhibitModel(project.Label,
                                     project.Namespace,
                                     project.ProjectId,
                                     project.ProjectName,
                                     project.Summary,
                                     project.Thumbnail ?? AssetUriIndex.DIRT_IMAGE,
                                     project.Author,
                                     project.Tags,
                                     project.UpdatedAt,
                                     project.DownloadCount,
                                     project.Reference);
        var installed =
            profile.Setup.Packages.FirstOrDefault(y => PackageHelper.IsMatched(y.Purl,
                                                                               project.Label,
                                                                               project.Namespace,
                                                                               project.ProjectId));
        if (installed is not null)
        {
            model.State = installed.Source == null || installed.Source != Basic.Source
                              ? ExhibitState.Editable
                              : ExhibitState.Locked;
            model.Installed = installed;
            // HACK: 为了优化性能，这里不获取 VersionName，而是在为 ExhibitPackageModal 弹出前加载数据时一并获取
            if (PackageHelper.TryParse(installed.Purl, out var parsed))
                model.InstalledVersionId = parsed.Vid;
        }

        return model;
    }

    #region Collections

    public SourceCache<ExhibitModel, ExhibitModel> PendingPackagesSource { get; } = new(x => x);
    public ReadOnlyObservableCollection<ExhibitModel> AddingPackagesView { get; }
    public ReadOnlyObservableCollection<ExhibitModel> ModifyingPackagesView { get; }
    public ReadOnlyObservableCollection<ExhibitModel> RemovingPackagesView { get; }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial InstanceBasicModel Basic { get; set; }

    [ObservableProperty]
    public partial RepositoryBasicModel SelectedRepository { get; set; }

    [ObservableProperty]
    public partial Filter Filter { get; set; } = Filter.Empty;

    [ObservableProperty]
    public partial string QueryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsFilterEnabled { get; set; }

    partial void OnIsFilterEnabledChanged(bool value)
    {
        if (value)
            Filter = Filter with
            {
                Loader = Basic.Loader != null && LoaderHelper.TryParse(Basic.Loader, out var loader)
                             ? loader.Identity
                             : null,
                Version = Basic.Version
            };
        else
            Filter = Filter with { Loader = null, Version = null };
    }

    [ObservableProperty]
    public partial ResourceKind? SelectedKind { get; set; }

    partial void OnSelectedKindChanged(ResourceKind? value)
    {
        if (value != null)
            Filter = Filter with { Kind = value };
        _ = SearchAsync();
    }

    [ObservableProperty]
    public partial InfiniteCollection<ExhibitModel>? Exhibits { get; set; }

    #endregion

    #region Injected

    private readonly RepositoryAgent _agent;
    private readonly DataService _dataService;
    private readonly ProfileManager _profileManager;
    private readonly NotificationService _notificationService;
    private readonly OverlayService _overlayService;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task SearchAsync()
    {
        try
        {
            var handle = await _agent.SearchAsync(SelectedRepository.Label, QueryText, Filter);
            var source = new InfiniteCollection<ExhibitModel>(async i =>
            {
                handle.PageIndex = (uint)(i < 0 ? 0 : i);
                try
                {
                    var profile = _profileManager.GetImmutable(Basic.Key);
                    // TODO: 具有三种状态
                    //  锁定（存在于构建中但锁定而无法操作）
                    //  已安装（存在于构建中且可以操作）
                    //  待添加（不存在，但位于待定区）
                    //  待移除（存在于构建，并位于待定区具有移除标记）
                    //  待修改（存在于构建，并位于待定区具有不同版本选择）

                    var rv = await handle.FetchAsync();
                    var tasks = rv
                               .Select(x =>
                                {
                                    var found = PendingPackagesSource.Items.FirstOrDefault(y => x.Label == y.Label
                                     && x.Namespace == y.Ns
                                     && x.Pid == y.ProjectId);
                                    if (found != null)
                                        return found;
                                    var model = new ExhibitModel(x.Label,
                                                                 x.Namespace,
                                                                 x.Pid,
                                                                 x.Name,
                                                                 x.Summary,
                                                                 x.Thumbnail ?? AssetUriIndex.DIRT_IMAGE,
                                                                 x.Author,
                                                                 x.Tags,
                                                                 x.UpdatedAt,
                                                                 x.DownloadCount,
                                                                 x.Reference);
                                    var installed =
                                        profile.Setup.Packages.FirstOrDefault(y => PackageHelper.IsMatched(y.Purl,
                                                                                  x.Label,
                                                                                  x.Namespace,
                                                                                  x.Pid));
                                    if (installed is not null)
                                    {
                                        model.State = installed.Source == null || installed.Source != Basic.Source
                                                          ? ExhibitState.Editable
                                                          : ExhibitState.Locked;
                                        model.Installed = installed;
                                        // HACK: 为了优化性能，这里不获取 VersionName，而是在为 ExhibitPackageModal 弹出前加载数据时一并获取
                                        if (PackageHelper.TryParse(installed.Purl, out var parsed))
                                            model.InstalledVersionId = parsed.Vid;
                                    }

                                    return model;
                                })
                               .ToArray();
                    return tasks;
                }
                catch (ApiException ex)
                {
                    _notificationService.PopMessage(ex, "Network unreachable", NotificationLevel.Warning);
                    Debug.WriteLine(ex);
                }

                return [];
            });
            Exhibits = source;
        }
        catch (ApiException ex)
        {
            _notificationService.PopMessage("Network unreachable", level: NotificationLevel.Warning);
            Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private async Task ViewPackage(ExhibitModel? exhibit)
    {
        if (exhibit is not null && _profileManager.TryGetMutable(Basic.Key, out var guard))
            try
            {
                var project = await _dataService.QueryProjectAsync(exhibit.Label, exhibit.Ns, exhibit.ProjectId);

                // 非 Unspecific
                if (exhibit.InstalledVersionId != null)
                    try
                    {
                        var package = await _dataService
                                           .ResolvePackageAsync(exhibit.Label,
                                                                exhibit.Ns,
                                                                exhibit.ProjectId,
                                                                exhibit.InstalledVersionId,
                                                                Filter.Empty)
                                           .ConfigureAwait(false);
                        exhibit.InstalledVersionName = package.VersionName;
                    }
                    catch
                    {
                        // 获取失败，可能是网络问题，也可能是 Purl 本身写的就有问题
                        // 这里给 VersionName 一个坏值
                        exhibit.InstalledVersionName = exhibit.InstalledVersionId;
                        // 这里可以打一下日志，但是懒得加了
                    }


                var model = new ExhibitPackageModel(project.Label,
                                                    project.Namespace,
                                                    project.ProjectId,
                                                    project.ProjectName,
                                                    project.Author,
                                                    project.Label,
                                                    project.Reference,
                                                    project.Thumbnail,
                                                    project.Tags,
                                                    project.DownloadCount,
                                                    project.Summary,
                                                    project.UpdatedAt,
                                                    project.Gallery.Select(x => x.Url).ToList());

                _overlayService.PopModal(new ExhibitPackageModal
                {
                    DataContext = model,
                    Exhibit = exhibit,
                    DataService = _dataService,
                    Filter = new Filter(Basic.Version,
                                        Basic.Loader != null
                                     && LoaderHelper.TryParse(Basic.Loader, out var loader)
                                            ? loader.Identity
                                            : null,
                                        project.Kind),
                    ViewPackageCommand = ViewPackageCommand,
                    ModifyPendingCallback = ModifyPending,
                    LinkExhibitCallback = LinkExhibit
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _notificationService.PopMessage(ex, "Failed to load project information", NotificationLevel.Warning);
            }
    }

    [RelayCommand]
    private void DismissPending()
    {
        foreach (var model in PendingPackagesSource.Items)
            model.State = model.Installed == null ? null : ExhibitState.Editable;
        PendingPackagesSource.Clear();
    }

    [RelayCommand]
    private async Task CollectPending()
    {
        if (_profileManager.TryGetMutable(Basic.Key, out var guard))
        {
            foreach (var model in PendingPackagesSource.Items)
                if (model.State == ExhibitState.Adding)
                {
                    guard.Value.Setup.Packages.Add(new Profile.Rice.Entry(PackageHelper.ToPurl(model.Label,
                                                                              model.Ns,
                                                                              model.ProjectId,
                                                                              model.PendingVersionId),
                                                                          true,
                                                                          null,
                                                                          []));
                    model.State = ExhibitState.Editable;
                }
                else if (model is { State: ExhibitState.Removing, Installed: not null })
                {
                    var exist = guard.Value.Setup.Packages.FirstOrDefault(x => x.Purl == model.Installed.Purl);
                    if (exist != null)
                        guard.Value.Setup.Packages.Remove(exist);
                    model.State = null;
                }
                else if (model is { State: ExhibitState.Modifying, Installed: not null })
                {
                    var exist = guard.Value.Setup.Packages.FirstOrDefault(x => x.Purl == model.Installed.Purl);
                    if (exist != null)
                        exist.Purl = PackageHelper.ToPurl(model.Label,
                                                          model.Ns,
                                                          model.ProjectId,
                                                          model.PendingVersionId);
                    model.State = ExhibitState.Editable;
                }

            await guard.DisposeAsync();
            PendingPackagesSource.Clear();
        }
    }

    #endregion
}