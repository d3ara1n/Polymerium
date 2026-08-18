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
using Polymerium.Avalonia.Services;
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
                                    : "Enum_Vanilla";
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

        var primary = _session.PrimaryCollectAction;
        CollectCommand = new AsyncRelayCommand(() => ExecuteCollectAsync(primary), () => CanCollect(primary));
        PrimaryAction = new(primary.LangKey, primary.Icon, CollectCommand);

        List<ExplorerActionItemModel> items = [];
        List<IAsyncRelayCommand> commands = [CollectCommand];
        foreach (var action in _session.SecondaryCollectActions)
        {
            var command = new AsyncRelayCommand(() => ExecuteCollectAsync(action), () => CanCollect(action));
            commands.Add(command);
            items.Add(new(action.LangKey, action.Icon, command));
        }

        SecondaryActions = items;
        HasSecondaryActions = items.Count > 0;

        // NOTE: 命令的 CanExecute 依赖待定区快照，而快照是传入命令的数组、不会通知命令，
        //  必须在待定区变化时显式 requery
        IRelayCommand[] requeryCommands = [.. commands, DismissPendingCommand];
        PendingPackagesSource
           .Connect()
           .QueryWhenChanged()
           .Subscribe(_ =>
            {
                foreach (var command in requeryCommands)
                {
                    command.NotifyCanExecuteChanged();
                }
            })
           .DisposeWith(_subscriptions);
    }

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

    #region Direct

    public IEnumerable<RepositoryBasicModel> Repositories { get; }

    public string Title => _session.Title;

    public Bitmap? Background => _session.Background;

    public bool IsFilterVisible => _session.InitialFilter is not null;

    public string? FilterLoaderLabel { get; }

    public string? FilterVersionLabel { get; }

    public ExplorerActionItemModel PrimaryAction { get; }

    public IAsyncRelayCommand CollectCommand { get; }

    public IReadOnlyList<ExplorerActionItemModel> SecondaryActions { get; }

    public bool HasSecondaryActions { get; }

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

    private async Task ExecuteCollectAsync(ExplorerActionModel action)
    {
        if (await action.Handler([.. PendingPackagesSource.Items]))
        {
            PendingPackagesSource.Clear();
        }
    }

    private bool CanCollect(ExplorerActionModel action) =>
        PendingPackagesSource.Count > 0 && (action.CanExecute?.Invoke([.. PendingPackagesSource.Items]) ?? true);

    #endregion

    #region Collections

    public SourceCache<ExhibitModel, ProjectIdentifier> PendingPackagesSource { get; } =
        new(x => new(x.Label, x.Namespace, x.ProjectId));

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
            Filter = Filter with { Loader = initial.Loader, Version = initial.Version };
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
                    // NOTE: ExhibitState 语义：锁定=在构建中但被锁；已安装=在构建中可操作；
                    //  待添加=不存在但已入待定区；待移除/待修改=在构建中且待定区有移除/改版标记。

                    var rv = await handle.FetchAsync(token);
                    var tasks = rv
                               .Select(x =>
                                {
                                    var existing = PendingPackagesSource.Lookup(new(x.Label, x.Namespace, x.Pid));
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
                    _notificationService.PopMessage(ex, LanguageManager.Instance.Error_BadNetwork.Current(), GrowlLevel.Warning);
                    Debug.WriteLine(ex);
                }
                catch (HttpRequestException ex)
                {
                    _notificationService.PopMessage(ex, LanguageManager.Instance.Error_BadNetwork.Current(), GrowlLevel.Warning);
                    Debug.WriteLine(ex);
                }

                return [];
            });
            Exhibits = source;
        }
        catch (ApiException ex)
        {
            _notificationService.PopMessage(ex, LanguageManager.Instance.Error_BadNetwork.Current(), GrowlLevel.Warning);
            Debug.WriteLine(ex);
        }
        catch (HttpRequestException ex)
        {
            _notificationService.PopMessage(ex, LanguageManager.Instance.Error_BadNetwork.Current(), GrowlLevel.Warning);
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
                                            LanguageManager.Instance.ExplorerPage_LoadProjectInformationDangerNotificationTitle.Current(),
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

    [RelayCommand(CanExecute = nameof(CanDismissPending))]
    private void DismissPending()
    {
        foreach (var model in PendingPackagesSource.Items)
        {
            _session.RevertState(model);
        }

        PendingPackagesSource.Clear();
    }

    private bool CanDismissPending() => PendingPackagesSource.Count > 0;

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

    #endregion
}
