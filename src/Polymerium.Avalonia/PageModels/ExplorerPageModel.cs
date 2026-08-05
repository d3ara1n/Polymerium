using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Exceptions;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using Refit;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;
using TridentCore.Pref;

namespace Polymerium.Avalonia.PageModels;

public partial class ExplorerPageModel : ViewModelBase
{
    public ExplorerPageModel(
        IViewContext<ExplorerSession> context,
        RepositoryAgent agent,
        DataService dataService,
        NotificationService notificationService,
        PersistenceService persistenceService)
    {
        _agent = agent;
        _dataService = dataService;
        _notificationService = notificationService;
        _persistenceService = persistenceService;

        if (context.Parameter is not { } session)
        {
            throw new PageNotReachedException(typeof(ExplorerPage), "Explorer session is not provided");
        }

        _session = session;
        _session.Validate();
        if (session.InitialFilter is { } initial)
        {
            IsFilterEnabled = true;
            FilterLoaderLabel = initial.Loader != null && LoaderHelper.TryParse(initial.Loader, out var loader)
                                    ? LoaderHelper.ToDisplayName(loader.Identity)
                                    : Resources.Enum_Vanilla;
            FilterVersionLabel = initial.Version;
        }

        var r = agent.Labels.Select(x => new RepositoryBasicModel(x, x.ToString().ToUpper())).ToList();
        Repositories = r;
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

    #region Direct

    public IEnumerable<RepositoryBasicModel> Repositories { get; }

    public string Title => _session.Title;

    public Bitmap? Background => _session.Background;

    public bool IsFilterVisible => _session.InitialFilter is not null;

    public string? FilterLoaderLabel { get; }

    public string? FilterVersionLabel { get; }

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

    #region Fields

    private readonly CompositeDisposable _subscriptions = new();
    private bool _suppressSearchOnKindChange;

    #endregion

    #region Other

    private void ModifyPending(ExhibitModel model)
    {
        if (model.State is null or ExhibitState.Editable)
        {
            PendingPackagesSource.RemoveKey(KeyOf(model));
        }
        else
        {
            PendingPackagesSource.AddOrUpdate(model);
        }
    }

    private static ProjectIdentifier KeyOf(ExhibitModel model) => new(model.Label, model.Namespace, model.ProjectId);

    private ExhibitModel? FindExisting(ProjectIdentifier identifier)
    {
        if (PendingPackagesSource.Lookup(identifier) is { HasValue: true } pending)
        {
            pending.Value.IsFavorite =
                _persistenceService.IsFavoriteProject(identifier.Repository, identifier.Namespace, identifier.Identity);
            return pending.Value;
        }

        var found = Exhibits?.FirstOrDefault(x => KeyOf(x) == identifier);
        if (found is not null)
        {
            found.IsFavorite =
                _persistenceService.IsFavoriteProject(identifier.Repository, identifier.Namespace, identifier.Identity);
        }

        return found;
    }

    #endregion

    #region Collections

    public SourceCache<ExhibitModel, ProjectIdentifier> PendingPackagesSource { get; } = new(x => new ProjectIdentifier(x.Label, x.Namespace, x.ProjectId));
    public ReadOnlyObservableCollection<ExhibitModel> AddingPackagesView { get; }
    public ReadOnlyObservableCollection<ExhibitModel> ModifyingPackagesView { get; }
    public ReadOnlyObservableCollection<ExhibitModel> RemovingPackagesView { get; }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial RepositoryBasicModel? SelectedRepository { get; set; }

    partial void OnSelectedRepositoryChanged(RepositoryBasicModel? value)
    {
        if (value is null)
        {
            return;
        }

        // NOTE: 资源类型选择器是 TabStrip，其 SelectionMode 为 AlwaysSelected。切仓库会替换它的
        //  ItemsSource，SelectionModel 会先 Clear 再强制 SelectedIndex=0（各仓库 Kinds 的首项恒为
        //  Mod），经 TwoWay 把 Mod 回写到 SelectedKind 并触发一次多余的 Mod 搜索，覆盖正确结果。
        //  所以这里先记下用户当前的选择、压住这次多余搜索，等绑定平息后再按新仓库重断言。
        var desiredKind = SelectedKind;
        _suppressSearchOnKindChange = true;

        Dispatcher.UIThread.Post(() =>
        {
            _suppressSearchOnKindChange = false;

            var target = value.Kinds?.Any(x => x == desiredKind) is true
                             ? desiredKind
                             : value.Kinds?.FirstOrDefault() ?? desiredKind;

            if (SelectedKind != target)
            {
                SelectedKind = target;
            }
            else
            {
                _ = SearchAsync();
            }
        });
    }

    [ObservableProperty]
    public partial Filter Filter { get; set; } = Filter.None with { Kind = ResourceKind.Mod };

    [ObservableProperty]
    public partial string QueryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsFilterEnabled { get; set; }

    partial void OnIsFilterEnabledChanged(bool value)
    {
        if (value && _session.InitialFilter is { } initial)
        {
            Filter = Filter with
            {
                Loader = initial.Loader,
                Version = initial.Version
            };
        }
        else
        {
            Filter = Filter with { Loader = null, Version = null };
        }

        _ = SearchAsync();
    }

    [ObservableProperty]
    public partial ResourceKind? SelectedKind { get; set; }

    partial void OnSelectedKindChanged(ResourceKind? value)
    {
        if (value == null)
        {
            return;
        }

        Filter = Filter with { Kind = value };

        // NOTE: 切仓库时 TabStrip 的强制回写也会走到这里，那次搜索由 OnSelectedRepositoryChanged 统一发起。
        if (!_suppressSearchOnKindChange)
        {
            _ = SearchAsync();
        }
    }

    [ObservableProperty]
    public partial InfiniteCollection<ExhibitModel>? Exhibits { get; set; }

    #endregion

    #region Injected

    private readonly ExplorerSession _session;
    private readonly RepositoryAgent _agent;
    private readonly DataService _dataService;
    private readonly NotificationService _notificationService;
    private readonly PersistenceService _persistenceService;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (SelectedRepository is null)
        {
            return;
        }

        if (Exhibits is { IsFetching: true })
        {
            return;
        }

        try
        {
            var handle = await _agent.SearchAsync(SelectedRepository.Label, QueryText, Filter);
            var source = new InfiniteCollection<ExhibitModel>(async (i, token) =>
            {
                handle.PageIndex = (uint)(i < 0 ? 0 : i);
                try
                {
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
                                    var existing = PendingPackagesSource.Lookup(new ProjectIdentifier(x.Label,
                                                                                                     x.Namespace,
                                                                                                     x.Pid));
                                    if (existing.HasValue)
                                    {
                                        existing.Value.IsFavorite =
                                            _persistenceService.IsFavoriteProject(x.Label, x.Namespace, x.Pid);
                                        return existing.Value;
                                    }

                                    return _session.BuildExhibit(x);
                                })
                               .ToArray();
                    return tasks;
                }
                catch (ApiException ex)
                {
                    _notificationService.PopMessage(ex, Resources.Error_BadNetwork, GrowlLevel.Warning);
                    Debug.WriteLine(ex);
                }
                catch (HttpRequestException ex)
                {
                    _notificationService.PopMessage(ex, Resources.Error_BadNetwork, GrowlLevel.Warning);
                    Debug.WriteLine(ex);
                }

                return [];
            });
            Exhibits = source;
        }
        catch (ApiException ex)
        {
            _notificationService.PopMessage(ex, Resources.Error_BadNetwork, GrowlLevel.Warning);
            Debug.WriteLine(ex);
        }
        catch (HttpRequestException ex)
        {
            _notificationService.PopMessage(ex, Resources.Error_BadNetwork, GrowlLevel.Warning);
            Debug.WriteLine(ex);
        }
    }

    [RelayCommand]
    private async Task ViewPackageAsync(ExhibitModel? exhibit)
    {
        if (exhibit is null)
        {
            return;
        }

        try
        {
            await _session.ViewExhibitAsync(exhibit, ModifyPending, FindExisting);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex,
                                            Resources
                                               .ExplorerPage_LoadProjectInformationDangerNotificationTitle,
                                            GrowlLevel.Warning,
                                            exhibit.Thumbnail);
        }
    }

    private static bool CanInstallPackage(ExhibitModel? exhibit) => exhibit?.State is null;

    [RelayCommand(CanExecute = nameof(CanInstallPackage))]
    private void InstallPackage(ExhibitModel? exhibit)
    {
        if (exhibit is null)
        {
            return;
        }

        exhibit.PendingVersionId = null;
        exhibit.PendingVersionName = null;
        exhibit.State = ExhibitState.Adding;
        ModifyPending(exhibit);
    }

    [RelayCommand]
    private async Task FavoritePackageAsync(ExhibitModel? exhibit)
    {
        if (SelectedRepository is null)
        {
            return;
        }

        if (exhibit is null)
        {
            return;
        }

        if (exhibit.IsFavorite)
        {
            _persistenceService.RemoveFavoriteProject(exhibit.Label, exhibit.Namespace, exhibit.ProjectId);
            exhibit.IsFavorite = false;
            if (SelectedRepository.Label == "favorite")
            {
                _ = SearchAsync();
            }

            return;
        }

        var project = await _dataService.QueryProjectAsync(new(exhibit.Label, exhibit.Namespace, exhibit.ProjectId));
        _persistenceService.AddFavoriteProject(project);
        exhibit.IsFavorite = true;
    }

    [RelayCommand]
    private void DismissPending()
    {
        foreach (var model in PendingPackagesSource.Items)
        {
            _session.RevertState(model);
        }

        PendingPackagesSource.Clear();
    }

    [RelayCommand]
    private void RemoveFromPending(ExhibitModel? exhibit)
    {
        if (exhibit is null)
        {
            return;
        }

        _session.RevertState(exhibit);
        exhibit.PendingVersionId = null;
        exhibit.PendingVersionName = null;

        PendingPackagesSource.RemoveKey(KeyOf(exhibit));
    }

    [RelayCommand]
    private async Task CollectPendingAsync()
    {
        if (await _session.CollectAsync(PendingPackagesSource.Items.ToArray()))
        {
            PendingPackagesSource.Clear();
        }
    }

    #endregion
}
