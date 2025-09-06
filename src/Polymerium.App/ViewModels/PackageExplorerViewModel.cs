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
using Polymerium.App.Properties;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Trident.Core.Services;
using Refit;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.ViewModels;

public partial class PackageExplorerViewModel : ViewModelBase
{
    #region Fields

    private readonly CompositeDisposable _subscriptions = new();

    #endregion

    public PackageExplorerViewModel(
        ViewBag bag,
        RepositoryAgent agent,
        DataService dataService,
        ProfileManager profileManager,
        NotificationService notificationService,
        OverlayService overlayService,
        PersistenceService persistenceService)
    {
        _agent = agent;
        _dataService = dataService;
        _profileManager = profileManager;
        _notificationService = notificationService;
        _overlayService = overlayService;
        _persistenceService = persistenceService;

        if (bag.Parameter is string key)
        {
            if (profileManager.TryGetImmutable(key, out var profile))
            {
                Basic = new(key, profile.Name, profile.Setup.Version, profile.Setup.Loader, profile.Setup.Source);
            }
            else
            {
                throw new PageNotReachedException(typeof(InstanceView),
                                                  Resources.InstanceView_KeyNotFoundExceptionMessage
                                                           .Replace("{0}", key));
            }
        }
        else
        {
            throw new PageNotReachedException(typeof(InstanceView), "Key to the instance is not provided");
        }


        var r = agent.Labels.Select(x => new RepositoryBasicModel(x, x.ToString().ToUpper())).ToList();
        Repositories = r;
        IsFilterEnabled = true;
        SelectedRepository = r.First();
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

    #region Properties

    public IEnumerable<RepositoryBasicModel> Repositories { get; }

    #endregion

    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        foreach (var repository in Repositories)
        {
            if (repository.Loaders == null || repository.Versions == null)
            {
                var status = await _dataService.CheckStatusAsync(repository.Label);
                repository.Kinds = [.. status.SupportedKinds.Where(x => x != ResourceKind.Modpack)];
            }
        }
    }

    #endregion

    #region Other

    private void ModifyPending(ExhibitModel model)
    {
        if (model.State is null or ExhibitState.Editable)
        {
            PendingPackagesSource.RemoveKey(model);
        }
        else
        {
            PendingPackagesSource.AddOrUpdate(model);
        }
    }

    private ExhibitModel LinkExhibit(Project project)
    {
        var foundInPending = PendingPackagesSource.Items.FirstOrDefault(x => x.Label == project.Label
                                                                          && x.Namespace == project.Namespace
                                                                          && x.ProjectId == project.ProjectId);
        if (foundInPending != null)
        {
            return foundInPending;
        }

        var foundInResult = Exhibits?.FirstOrDefault(x => x.Label == project.Label
                                                       && x.Namespace == project.Namespace
                                                       && x.ProjectId == project.ProjectId);
        if (foundInResult != null)
        {
            return foundInResult;
        }

        var profile = _profileManager.GetImmutable(Basic.Key);
        var model = new ExhibitModel(project.Label,
                                     project.Namespace,
                                     project.ProjectId,
                                     project.ProjectName,
                                     project.Summary,
                                     project.Thumbnail ?? AssetUriIndex.DirtImage,
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
            {
                model.InstalledVersionId = parsed.Vid;
            }
        }

        return model;
    }

    private async Task SearchInternalAsync()
    {
        try
        {
            var handle = await _agent.SearchAsync(SelectedRepository.Label, QueryText, Filter);
            var source = new InfiniteCollection<ExhibitModel>(async (i, token) =>
            {
                handle.PageIndex = (uint)(i < 0 ? 0 : i);
                try
                {
                    var profile = _profileManager.GetImmutable(Basic.Key);
                    // 具有三种状态
                    // 锁定（存在于构建中但锁定而无法操作）
                    // 已安装（存在于构建中且可以操作）
                    // 待添加（不存在，但位于待定区）
                    // 待移除（存在于构建，并位于待定区具有移除标记）
                    // 待修改（存在于构建，并位于待定区具有不同版本选择）

                    var rv = await handle.FetchAsync(token);
                    var tasks = rv
                               .Select(x =>
                                {
                                    var found = PendingPackagesSource.Items.FirstOrDefault(y => x.Label == y.Label
                                     && x.Namespace == y.Namespace
                                     && x.Pid == y.ProjectId);
                                    if (found != null)
                                    {
                                        return found;
                                    }

                                    var model = new ExhibitModel(x.Label,
                                                                 x.Namespace,
                                                                 x.Pid,
                                                                 x.Name,
                                                                 x.Summary,
                                                                 x.Thumbnail ?? AssetUriIndex.DirtImage,
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
                                        {
                                            model.InstalledVersionId = parsed.Vid;
                                        }
                                    }

                                    return model;
                                })
                               .ToArray();
                    return tasks;
                }
                catch (ApiException ex)
                {
                    _notificationService.PopMessage(ex, "Network unreachable", GrowlLevel.Warning);
                    Debug.WriteLine(ex);
                }

                return [];
            });
            Exhibits = source;
        }
        catch (ApiException ex)
        {
            _notificationService.PopMessage("Network unreachable", level: GrowlLevel.Warning);
            Debug.WriteLine(ex);
        }
    }

    #endregion

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

    partial void OnSelectedRepositoryChanged(RepositoryBasicModel value)
    {
        // HACK: 此时 SelectedKind 没回绑，Filter 未更新，因此手动提前打补丁
        if (value.Kinds?.Any(x => x == Filter.Kind) is not true)
        {
            Filter = Filter with { Kind = value.Kinds?.FirstOrDefault() ?? Filter.Kind };
        }

        _ = SearchInternalAsync();
    }

    [ObservableProperty]
    public partial Filter Filter { get; set; } = Filter.None with { Kind = ResourceKind.Mod };

    [ObservableProperty]
    public partial string QueryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsFilterEnabled { get; set; }

    partial void OnIsFilterEnabledChanged(bool value)
    {
        if (value)
        {
            Filter = Filter with
            {
                Loader = Basic.Loader != null && LoaderHelper.TryParse(Basic.Loader, out var loader)
                             ? loader.Identity
                             : null,
                Version = Basic.Version
            };
        }
        else
        {
            Filter = Filter with { Loader = null, Version = null };
        }
    }

    [ObservableProperty]
    public partial ResourceKind? SelectedKind { get; set; }

    partial void OnSelectedKindChanged(ResourceKind? value)
    {
        if (value != null)
        {
            Filter = Filter with { Kind = value };
        }
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
    private readonly PersistenceService _persistenceService;

    #endregion

    #region Commands

    [RelayCommand]
    private void Search() => _ = SearchInternalAsync();

    [RelayCommand]
    private async Task ViewPackageAsync(ExhibitModel? exhibit)
    {
        if (exhibit is not null)
        {
            try
            {
                var project = await _dataService.QueryProjectAsync(exhibit.Label, exhibit.Namespace, exhibit.ProjectId);

                // 非 Unspecific
                if (exhibit.InstalledVersionId != null)
                {
                    var package = await _dataService.ResolvePackageAsync(exhibit.Label,
                                                                         exhibit.Namespace,
                                                                         exhibit.ProjectId,
                                                                         exhibit.InstalledVersionId,
                                                                         Filter.None);
                    exhibit.InstalledVersionName = package.VersionName;
                }


                var model = new ExhibitPackageModel(project.Label,
                                                    project.Namespace,
                                                    project.ProjectId,
                                                    project.ProjectName,
                                                    project.Author,
                                                    project.Reference,
                                                    project.Thumbnail,
                                                    project.Tags,
                                                    project.DownloadCount,
                                                    project.Summary,
                                                    project.UpdatedAt,
                                                    [.. project.Gallery.Select(x => x.Url)]);

                _overlayService.PopModal(new ExhibitPackageModal
                {
                    DataContext = model,
                    Exhibit = exhibit,
                    DataService = _dataService,
                    Filter = new(Basic.Version,
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
                _notificationService.PopMessage(ex, "Failed to load project information", GrowlLevel.Warning);
            }
        }
    }

    [RelayCommand]
    private void DismissPending()
    {
        foreach (var model in PendingPackagesSource.Items)
        {
            model.State = model.Installed == null ? null : ExhibitState.Editable;
        }

        PendingPackagesSource.Clear();
    }

    [RelayCommand]
    private async Task CollectPendingAsync()
    {
        if (_profileManager.TryGetMutable(Basic.Key, out var guard))
        {
            foreach (var model in PendingPackagesSource.Items)
            {
                switch (model)
                {
                    case { State: ExhibitState.Adding }:
                    {
                        var entry = new Profile.Rice.Entry(PackageHelper.ToPurl(model.Label,
                                                                                    model.Namespace,
                                                                                    model.ProjectId,
                                                                                    model.PendingVersionId),
                                                           true,
                                                           null,
                                                           []);
                        _persistenceService.AppendAction(new(Basic.Key,
                                                             PersistenceService.ActionKind.EditPackage,
                                                             null,
                                                             entry.Purl));
                        guard.Value.Setup.Packages.Add(entry);
                        model.State = ExhibitState.Editable;
                        model.Installed = entry;
                        model.InstalledVersionName = model.PendingVersionName;
                        model.InstalledVersionId = model.PendingVersionId;
                        break;
                    }
                    case { State: ExhibitState.Removing, Installed: not null }:
                    {
                        var exist = guard.Value.Setup.Packages.FirstOrDefault(x => x.Purl == model.Installed.Purl);
                        if (exist != null)
                        {
                            guard.Value.Setup.Packages.Remove(exist);
                        }

                        _persistenceService.AppendAction(new(Basic.Key,
                                                             PersistenceService.ActionKind.EditPackage,
                                                             model.Installed.Purl,
                                                             null));
                        model.State = null;
                        model.Installed = null;
                        model.InstalledVersionName = null;
                        model.InstalledVersionId = null;
                        break;
                    }
                    case { State: ExhibitState.Modifying, Installed: not null }:
                    {
                        var old = model.Installed.Purl;
                        model.Installed.Purl = PackageHelper.ToPurl(model.Label,
                                                                    model.Namespace,
                                                                    model.ProjectId,
                                                                    model.PendingVersionId);
                        _persistenceService.AppendAction(new(Basic.Key,
                                                             PersistenceService.ActionKind.EditPackage,
                                                             old,
                                                             model.Installed.Purl));
                        model.State = ExhibitState.Editable;
                        model.InstalledVersionName = model.PendingVersionName;
                        model.InstalledVersionId = model.PendingVersionId;
                        break;
                    }
                }
            }

            await guard.DisposeAsync();
            PendingPackagesSource.Clear();
        }
    }

    #endregion
}
