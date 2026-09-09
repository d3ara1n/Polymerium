using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Tasks;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using NotificationSidebar = Polymerium.Avalonia.Sidebars.NotificationSidebar;

namespace Polymerium.Avalonia;

public partial class MainWindowContext : ObservableObject
{
    public MainWindowContext(
        ProfileManager profileManager,
        InstanceManager instanceManager,
        InstanceStateAggregator aggregator,
        NotificationService notificationService,
        NavigationService navigationService,
        PersistenceService persistenceService,
        InstanceService instanceService,
        OverlayService overlayService,
        UpdateService updateService,
        ConfigurationService configurationService)
    {
        _profileManager = profileManager;
        _notificationService = notificationService;
        _navigationService = navigationService;
        _persistenceService = persistenceService;
        _instanceService = instanceService;
        _overlayService = overlayService;
        _updateService = updateService;
        _configurationService = configurationService;

        SubscribeProfileList(profileManager);
        SubscribeState(aggregator);

        // 顶栏未读徽标——转发 app 级 NotificationService 未读计数（数据归属仍在服务）。
        _notificationService.UnreadCountChanged += OnUnreadCountChanged;
        UnreadNotificationCount = _notificationService.UnreadCount;

        _updateService.StateChanged += OnUpdateStateChanged;

        _instanceService.PinnedChangeStream.Subscribe(OnPinnedChanged).DisposeWith(_disposables);

        _ = _entries
           .Connect()
           .SortAndBind(out var view,
                        SortExpressionComparer<InstanceEntryModel>
                           .Ascending(m => GetTier(m))
                           .ThenByDescending(x => x.LastPlayedAtRaw ?? DateTimeOffset.MinValue))
           .Subscribe()
           .DisposeWith(_disposables);
        View = view;
    }

    #region Other

    public void Navigate(Type page, object? parameter) => _navigationService.Navigate(page, parameter);

    #endregion

    #region Other reactive

    // NotificationService 事件均在 UI 线程触发（见服务注释），此处直接赋值即可。
    private void OnUnreadCountChanged(int count) => UnreadNotificationCount = count;

    #endregion

    #region Fields

    private readonly CompositeDisposable _disposables = new();
    private readonly SourceCache<InstanceEntryModel, string> _entries = new(x => x.Basic.Key);
    private readonly List<string> _recent = [];
    private int _recentCounter;

    #endregion

    #region Lifecycles

    public void OnInitialize()
    {
        if (Program.FirstRun)
        {
            _overlayService.PopModal(new OobeModal
            {
                ConfigurationService = _configurationService,
                OverlayService = _overlayService,
                NotificationService = _notificationService
            });
        }
    }

    public void OnDeinitialize()
    {
        _notificationService.ClearAll();
        _notificationService.UnreadCountChanged -= OnUnreadCountChanged;

        _disposables.Dispose();

        _profileManager.ProfileAdded -= OnProfileAdded;
        _profileManager.ProfileUpdated -= OnProfileUpdated;
        _profileManager.ProfileRemoved -= OnProfileRemoved;
    }

    #endregion

    #region Injected

    private readonly NotificationService _notificationService;
    private readonly NavigationService _navigationService;
    private readonly PersistenceService _persistenceService;
    private readonly InstanceService _instanceService;
    private readonly OverlayService _overlayService;
    private readonly UpdateService _updateService;
    private readonly ProfileManager _profileManager;
    private readonly ConfigurationService _configurationService;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial ReadOnlyObservableCollection<InstanceEntryModel> View { get; set; }

    [ObservableProperty]
    public partial int UnreadNotificationCount { get; set; }

    #endregion

    #region Commands

    [RelayCommand]
    private void ViewInstance(string? key)
    {
        if (key is not null)
        {
            _navigationService.Navigate<InstancePage>(key);
        }
    }

    [RelayCommand]
    private Task ExportInstanceAsync(string? key) => _instanceService.ExportInstanceAsync(key);

    [RelayCommand]
    private void Navigate(Type? page)
    {
        if (page != null)
        {
            Navigate(page, null);
        }
    }

    [RelayCommand]
    private Task ViewLog(InstanceActivity.Running? activity)
    {
        if (activity != null)
        {
            var path = Path.Combine(PathDef.Default.DirectoryOfBuild(activity.Key), "logs", "latest.log");
            if (File.Exists(path))
            {
                return TopLevelHelper.LaunchFileInfoAsync(TopLevelHelper.GetTopLevel(),
                                                          new(path),
                                                          LanguageManager.Instance.Shared_FailedToOpenLogFileDangerNotificationTitle.Current(),
                                                          _notificationService,
                                                          thumbnail: ThumbnailHelper.ForInstance(activity.Key));
            }

            _notificationService.PopMessage(LanguageManager.Instance.MainWindow_LogFileNotFoundWarningNotificationMessage.Current(),
                                            LanguageManager.Instance.Shared_FailedToOpenLogFileDangerNotificationTitle.Current(),
                                            GrowlLevel.Warning,
                                            thumbnail: ThumbnailHelper.ForInstance(activity.Key));
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Play(string key) => _instanceService.Play(key);

    [RelayCommand]
    private void Deploy(string key) => _instanceService.Deploy(key);

    [RelayCommand]
    private Task OpenFolder(string? key) => _instanceService.OpenFolder(key);

    [RelayCommand]
    private void GotoProperties(string? key) => _instanceService.GotoProperties(key);

    [RelayCommand]
    private void GotoSetup(string? key) => _instanceService.GotoSetup(key);

    [RelayCommand]
    private void OpenNotificationSidebar() => _overlayService.PopSidebar<NotificationSidebar>();

    #endregion

    #region Menu Commands

    [RelayCommand]
    private void About() => _overlayService.PopModal(new AboutModal());

    [RelayCommand]
    private void NewInstance() => _navigationService.Navigate<NewInstancePage>();

    [RelayCommand]
    private void GoHome() => _navigationService.Navigate<LandingPage>();

    [RelayCommand]
    private void GoMarketplace() => _navigationService.Navigate<MarketplacePortalPage>();

    [RelayCommand]
    private void GoAccounts() => _navigationService.Navigate<AccountsPage>();

    [RelayCommand]
    private void GoInstances() => _navigationService.Navigate<InstancesPage>();

    [RelayCommand]
    private void GoRecipes() => _navigationService.Navigate<RecipesPage>();

    [RelayCommand]
    private void GoStorage() => _navigationService.Navigate<MaintenanceStoragePage>();

    [RelayCommand]
    private void GoSettings() => _navigationService.Navigate<SettingsPage>();

    [RelayCommand]
    private void ToggleFullScreen()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
            {
                MainWindow: Window window
            })
        {
            window.WindowState = window.WindowState == WindowState.FullScreen
                                     ? WindowState.Normal
                                     : WindowState.FullScreen;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            await _updateService.CheckUpdateAsync();
            switch (_updateService.UpdateState)
            {
                case AppUpdateState.Found when _updateService.CurrentUpdate is { } update:
                    _notificationService.PopMessage(string.Format(LanguageManager.Instance.MainWindow_UpdateFoundNotificationMessage.Current(),
                                                                  update.Version),
                                                    LanguageManager.Instance.MainWindow_UpdateFoundNotificationTitle.Current(),
                                                    GrowlLevel.Success);
                    break;
                case AppUpdateState.Latest:
                    _notificationService.PopMessage(LanguageManager.Instance.MainWindow_UpdateLatestNotificationMessage.Current(),
                                                    LanguageManager.Instance.MainWindow_UpdateLatestNotificationTitle.Current());
                    break;
            }
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex, LanguageManager.Instance.MainWindow_CheckForUpdatesDangerNotificationTitle.Current());
        }
    }

    private bool CanCheckForUpdates => _updateService.CanCheckUpdate;

    // UpdateService 的状态迁移线程不保证为 UI 线程，故切回 UI 线程刷新。
    private void OnUpdateStateChanged() =>
        Dispatcher.UIThread.Post(() => CheckForUpdatesCommand.NotifyCanExecuteChanged());

    [RelayCommand]
    private async Task OpenGitHubAsync()
    {
        var topLevel =
            TopLevel.GetTopLevel((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                               ?.MainWindow);
        await TopLevelHelper.LaunchUriAsync(topLevel,
                                            new(Program.RepositoryUrl),
                                            LanguageManager.Instance.MainWindow_OpenGitHubDangerNotificationTitle.Current());
    }

    #endregion

    #region Profile Service

    internal void SubscribeProfileList(ProfileManager manager)
    {
        manager.ProfileAdded += OnProfileAdded;
        manager.ProfileUpdated += OnProfileUpdated;
        manager.ProfileRemoved += OnProfileRemoved;

        var list = new List<InstanceEntryModel>();
        foreach (var (key, item) in manager.Profiles)
        {
            if (!_instanceService.IsPinned(key))
            {
                continue;
            }

            list.Add(new(key, item.Name, item.Setup.Version, item.Setup.Loader, item.Setup.Source)
            {
                IsPinned = true,
                LastPlayedAtRaw =
                    DateTimeHelper.FromPersistedLocalDateTime(_persistenceService.GetLastActivity(key)
                                                                ?.End)
            });
        }

        _entries.AddOrUpdate(list);
    }

    private void OnProfileAdded(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        var defaultAccount = _persistenceService.GetDefaultAccount();
        if (defaultAccount != null)
        {
            _persistenceService.SetAccountSelector(e.Key, defaultAccount.Uuid);
        }

        Dispatcher.UIThread.Post(() =>
        {
            InstanceEntryModel entry;
            if (_entries.Lookup(e.Key) is { HasValue: true, Value: var existing })
            {
                existing.Basic.Name = e.Value.Name;
                existing.Basic.Source = e.Value.Setup.Source;
                existing.Basic.Version = e.Value.Setup.Version;
                existing.Basic.Loader = e.Value.Setup.Loader;
                existing.Basic.UpdateIcon();
                entry = existing;
            }
            else
            {
                entry = new(e.Key, e.Value.Name, e.Value.Setup.Version, e.Value.Setup.Loader, e.Value.Setup.Source);
                _entries.AddOrUpdate(entry);
            }

            AddRecent(e.Key, entry);
        });
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (_entries.Lookup(e.Key) is { HasValue: true, Value: var model })
            {
                model.Basic.Name = e.Value.Name;
                model.Basic.Source = e.Value.Setup.Source;
                model.Basic.Version = e.Value.Setup.Version;
                model.Basic.Loader = e.Value.Setup.Loader;
                model.Basic.UpdateIcon();
            }
        });

    private void OnProfileRemoved(object? sender, ProfileManager.ProfileChangedEventArgs e) =>
        Dispatcher.UIThread.Post(() =>
        {
            _recent.Remove(e.Key);
            if (_instanceService.IsPinned(e.Key))
            {
                _instanceService.Unpin(e.Key);
            }

            _entries.RemoveKey(e.Key);
        });

    #endregion

    #region Instance State

    internal void SubscribeState(InstanceStateAggregator aggregator) =>
        aggregator
           .StateChangeStream.Subscribe(change =>
            {
                foreach (var item in change)
                {
                    switch (item.Reason)
                    {
                        case ChangeReason.Add:
                        case ChangeReason.Update:
                            HandleSnapshotUpdate(item.Current);
                            break;
                        case ChangeReason.Remove:
                            HandleSnapshotRemove(item.Current);
                            break;
                    }
                }
            })
           .DisposeWith(_disposables);

    // NOTE: 活动值投递到 UI 线程后仍然有效（不可变），故查询与变更整体搬到 UI 线程：
    //  _entries 的读写因此同在一个线程，不会在“查完到改完”之间被另一条变更插进来。
    private void HandleSnapshotUpdate(InstanceActivity activity) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (_entries.Lookup(activity.Key) is { HasValue: true, Value: var model })
            {
                model.State = activity.Kind;
                model.IsPending = activity.Progress is ActivityProgress.Indeterminate;
                model.Progress = activity.Progress is ActivityProgress.Determinate d ? d.Percent : 0d;
                return;
            }

            // 从 InstancesPage 启动的未 pin/非 recent 实例，状态必须可见，拉进 _entries。
            InstanceEntryModel entry = _profileManager.TryGetImmutable(activity.Key, out var profile)
                                           ? new(activity.Key,
                                                 profile.Name,
                                                 profile.Setup.Version,
                                                 profile.Setup.Loader,
                                                 profile.Setup.Source)
                                           : new(activity.Key, activity.Key, "N/A", null, null);
            entry.State = activity.Kind;
            entry.IsPending = activity.Progress is ActivityProgress.Indeterminate;
            entry.Progress = activity.Progress is ActivityProgress.Determinate progress ? progress.Percent : 0d;
            _entries.AddOrUpdate(entry);
        });

    private void HandleSnapshotRemove(InstanceActivity activity) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (_entries.Lookup(activity.Key) is not { HasValue: true, Value: var model })
            {
                return;
            }

            model.State = InstanceState.Idle;
            model.IsPending = false;
            model.Progress = 0d;
            if (!ShouldShow(model))
            {
                _entries.Remove(model);
            }
        });

    private void OnPinnedChanged(IChangeSet<string, string> change) =>
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var item in change)
            {
                var pinned = item.Reason is ChangeReason.Add or ChangeReason.Update;
                if (_entries.Lookup(item.Key) is { HasValue: true, Value: var entry })
                {
                    entry.IsPinned = pinned;
                    if (!pinned && !ShouldShow(entry))
                    {
                        _entries.Remove(entry);
                    }
                }
                else if (pinned && _profileManager.TryGetImmutable(item.Key, out var profile))
                {
                    _entries.AddOrUpdate(new InstanceEntryModel(item.Key,
                                                                profile.Name,
                                                                profile.Setup.Version,
                                                                profile.Setup.Loader,
                                                                profile.Setup.Source)
                    {
                        IsPinned = true,
                        LastPlayedAtRaw =
                            DateTimeHelper.FromPersistedLocalDateTime(_persistenceService
                                                                     .GetLastActivity(item.Key)
                                                                    ?.End)
                    });
                }
            }
        });

    private void AddRecent(string key, InstanceEntryModel entry)
    {
        if (_recent.Contains(key))
        {
            return;
        }

        _recent.Add(key);
        entry.RecentOrder = ++_recentCounter;
        while (_recent.Count > 3)
        {
            var oldestKey = _recent[0];
            _recent.RemoveAt(0);
            if (_entries.Lookup(oldestKey) is { HasValue: true, Value: var stale })
            {
                stale.RecentOrder = null;
                if (!ShouldShow(stale))
                {
                    _entries.Remove(stale);
                }
            }
        }
    }

    private static int GetTier(InstanceEntryModel m) =>
        m.State == InstanceState.Idle ? m.IsPinned ? 1 : m.RecentOrder.HasValue ? 2 : 3 : 0;

    private static bool ShouldShow(InstanceEntryModel m) =>
        m.IsPinned || m.State != InstanceState.Idle || m.RecentOrder.HasValue;

    [RelayCommand]
    private void Pin(string? key)
    {
        if (key != null)
        {
            _instanceService.Pin(key);
        }
    }

    [RelayCommand]
    private void Unpin(string? key)
    {
        if (key != null)
        {
            _instanceService.Unpin(key);
        }
    }

    #endregion
}
