using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.PageModels;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Extensions;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Tasks;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Igniters;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using TridentCore.Core.Utilities;
using Velopack;
using NotificationSidebar = Polymerium.Avalonia.Sidebars.NotificationSidebar;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using TridentCore.Abstractions.Reactive;

namespace Polymerium.Avalonia;

public partial class MainWindowContext : ObservableObject
{

    #region Fields

    private readonly CompositeDisposable _disposables = new();
    private readonly SourceCache<InstanceEntryModel, string> _entries = new(x => x.Basic.Key);

    #endregion

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
        UpdateManager updateManager,
        ExporterAgent exporterAgent,
        ConfigurationService configurationService)
    {
        _profileManager = profileManager;
        _notificationService = notificationService;
        _navigationService = navigationService;
        _persistenceService = persistenceService;
        _instanceService = instanceService;
        _overlayService = overlayService;
        _updateService = updateService;
        _updateManager = updateManager;
        _exporterAgent = exporterAgent;
        _configurationService = configurationService;

        _updateService.SetHandler(OnUpdateFound);
        CurrentUpdate = _updateService.CurrentUpdate;

        SubscribeProfileList(profileManager);
        SubscribeState(aggregator);

        // 顶栏未读徽标：转发 app 级 NotificationService 的未读计数（数据归属仍在服务）
        _notificationService.UnreadCountChanged += OnUnreadCountChanged;
        UnreadNotificationCount = _notificationService.UnreadCount;

        var filter = this.WhenValueChanged(x => x.FilterText).Select(BuildFilter);
        _ = _entries
           .Connect()
           .Filter(filter)
           .SortAndBind(out var view,
                        SortExpressionComparer<InstanceEntryModel>.Descending(x => x.LastPlayedAtRaw
                                                                               ?? DateTimeOffset.MinValue))
           .Subscribe()
           .DisposeWith(_disposables);
        View = view;
    }

    private static Func<InstanceEntryModel, bool> BuildFilter(string? filter) =>
        x => string.IsNullOrEmpty(filter) || x.Basic.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);

    #region Lifecycles

    public void OnInitialize()
    {
        // Show OOBE modal for first-time users
        // OOBE now includes privilege check step on Windows
        if (Program.FirstRun)
        {
            _overlayService.PopModal(new OobeModal
            {
                ConfigurationService = _configurationService,
                OverlayService = _overlayService,
                NotificationService = _notificationService,
            });
        }
    }

    public void OnDeinitialize()
    {
        _updateService.SetHandler(null);
        _notificationService.ClearAll();
        _notificationService.UnreadCountChanged -= OnUnreadCountChanged;

        _disposables.Dispose();

        _profileManager.ProfileAdded -= OnProfileAdded;
        _profileManager.ProfileUpdated -= OnProfileUpdated;
        _profileManager.ProfileRemoved -= OnProfileRemoved;
    }

    #endregion

    #region Other

    public void Navigate(Type page, object? parameter) => _navigationService.Navigate(page, parameter);

    #endregion

    #region Injected

    private readonly NotificationService _notificationService;
    private readonly NavigationService _navigationService;
    private readonly PersistenceService _persistenceService;
    private readonly InstanceService _instanceService;
    private readonly OverlayService _overlayService;
    private readonly UpdateService _updateService;
    private readonly UpdateManager _updateManager;
    private readonly ProfileManager _profileManager;
    private readonly ExporterAgent _exporterAgent;
    private readonly ConfigurationService _configurationService;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial ReadOnlyObservableCollection<InstanceEntryModel> View { get; set; }

    [ObservableProperty]
    public partial string FilterText { get; set; } = string.Empty;


    [ObservableProperty]
    public partial AppUpdateModel? CurrentUpdate { get; set; }

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
    private async Task ExportInstanceAsync(string? key)
    {
        if (key is not null && _profileManager.TryGetImmutable(key, out var profile))
        {
            var loaderLabel = "None";
            if (profile.Setup.Loader is not null && LoaderHelper.TryParse(profile.Setup.Loader, out var loader))
            {
                loaderLabel = LoaderHelper.ToDisplayLabel(loader.Identity, loader.Version);
            }

            var overrideName = profile.GetOverride<string>(Profile.OVERRIDE_MODPACK_NAME);
            var overrideAuthor = profile.GetOverride<string>(Profile.OVERRIDE_MODPACK_AUTHOR);
            var overrideVersion = profile.GetOverride<string>(Profile.OVERRIDE_MODPACK_VERSION);

            var user = string.Empty;
            var account = _persistenceService.GetAccounts().FirstOrDefault(x => x.IsDefault);
            if (account != null)
            {
                user = AccountHelper.ToCooked(account).Username;
            }

            var dataPath = PathDef.Default.FileOfPackData(key);
            PackData? pack = null;
            try
            {
                if (File.Exists(dataPath))
                {
                    pack = JsonSerializer.Deserialize<PackData>(await File.ReadAllTextAsync(dataPath),
                                                                FileHelper.SerializerOptions);
                }
            }
            catch (Exception ex)
            {
                _notificationService.PopMessage(ex,
                                                Resources.MainWindow_ReadPackConfigDangerNotificationTitle,
                                                GrowlLevel.Warning,
                                                thumbnail: ThumbnailHelper.ForInstance(key));
            }

            pack ??= new();

            var availableTags = profile.Setup.Packages.SelectMany(x => x.Tags).Distinct().OrderBy(x => x).ToList();

            var dialog = new ModpackExporterDialog
            {
                Pack = pack,
                AvailableTags = availableTags,
                OverlayService = _overlayService,
                NameOriginal = !string.IsNullOrEmpty(overrideName) ? overrideName : profile.Name,
                LoaderLabel = loaderLabel,
                PackageCount = profile.Setup.Packages.Count,
                AuthorOriginal = !string.IsNullOrEmpty(overrideAuthor) ? overrideAuthor : user,
                VersionOriginal = !string.IsNullOrEmpty(overrideVersion) ? overrideVersion : "1.0.0",
                Result = new ModpackExporterModel(key),
            };

            if (await _overlayService.PopDialogAsync(dialog) && dialog.Result is ModpackExporterModel model)
            {
                var top = TopLevelHelper.GetTopLevel();
                var storage = top.StorageProvider;
                if (storage.CanOpen)
                {
                    var name = !string.IsNullOrEmpty(model.NameOverride) ? model.NameOverride : dialog.NameOriginal;
                    var author = !string.IsNullOrEmpty(model.AuthorOverride)
                                     ? model.AuthorOverride
                                     : dialog.AuthorOriginal;
                    var version = !string.IsNullOrEmpty(model.VersionOverride)
                                      ? model.VersionOverride
                                      : dialog.VersionOriginal;
                    var storageItem = await storage.SaveFilePickerAsync(new()
                    {
                        SuggestedStartLocation =
                            await storage
                               .TryGetWellKnownFolderAsync(WellKnownFolder
                                                              .Downloads),
                        SuggestedFileName =
                            $"{name}.{version}",
                        DefaultExtension = "zip",
                        FileTypeChoices =
                        [
                            new(Resources
                                       .Shared_ZipArchiveFileTypeText)
                                {
                                    Patterns = ["*.zip"],
                                },
                            ],
                    });
                    if (storageItem is not null)
                    {
                        profile.SetOverride(Profile.OVERRIDE_MODPACK_NAME, name);
                        profile.SetOverride(Profile.OVERRIDE_MODPACK_AUTHOR, author);
                        profile.SetOverride(Profile.OVERRIDE_MODPACK_VERSION, version);
                        var notification = _notificationService.PopProgress(name,
                                                                                Resources
                                                                                   .MainWindow_ExportModpackProgressingNotificationMessage,
                                                                                thumbnail: ThumbnailHelper
                                                                                   .ForInstance(key));
                        try
                        {
                            using var container = await Task.Run(async () => await _exporterAgent.ExportAsync(pack,
                                                                     model.SelectedExporterLabel,
                                                                     key,
                                                                     name,
                                                                     author,
                                                                     version));
                            notification.Report(50);
                            await using var stream = await storageItem.OpenWriteAsync();
                            await Task.Run(async () =>
                            {
                                await _exporterAgent.PackCompressedAsync(stream, container);
                            });
                            notification.Report(100);
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            var path = storageItem.TryGetLocalPath();
                            _notificationService.PopMessage(path ?? Resources.Enum_Unknown,
                                                            Resources
                                                               .MainWindow_ExportModpackSuccessNotificationTitle,
                                                            thumbnail: ThumbnailHelper.ForInstance(key));
                        }
                        catch (Exception ex)
                        {
                            _notificationService.PopMessage(ex,
                                                            Resources
                                                               .MainWindow_ExportModpackDangerNotificationTitle,
                                                            thumbnail: ThumbnailHelper.ForInstance(key));
                        }
                        finally
                        {
                            notification.Dispose();
                        }
                    }

                }
            }

            var dir = Path.GetDirectoryName(dataPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                await File.WriteAllTextAsync(dataPath, JsonSerializer.Serialize(pack, FileHelper.SerializerOptions));
            }
            catch (Exception ex)
            {
                _notificationService.PopMessage(ex,
                                                Resources.MainWindow_SavePackConfigDangerNotificationTitle,
                                                thumbnail: ThumbnailHelper.ForInstance(key));
            }
        }
    }

    [RelayCommand]
    private void Navigate(Type? page)
    {
        if (page != null)
        {
            Navigate(page, null);
        }
    }

    [RelayCommand]
    private Task ViewLog(LaunchTracker? tracker)
    {
        if (tracker != null)
        {
            var path = Path.Combine(PathDef.Default.DirectoryOfBuild(tracker.Key), "logs", "latest.log");
            if (File.Exists(path))
            {
                return TopLevelHelper.LaunchFileInfoAsync(TopLevelHelper.GetTopLevel(),
                                                          new(path),
                                                          Resources.Shared_FailedToOpenLogFileDangerNotificationTitle,
                                                          _notificationService,
                                                          thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
            }
            else
            {
                _notificationService.PopMessage(Resources.MainWindow_LogFileNotFoundWarningNotificationMessage,
                                                Resources.Shared_FailedToOpenLogFileDangerNotificationTitle,
                                                GrowlLevel.Warning,
                                                thumbnail: ThumbnailHelper.ForInstance(tracker.Key));
            }
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Play(string key)
    {
        try
        {
            _instanceService.DeployAndLaunch(key, LaunchMode.Managed);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex,
                                            Resources.Shared_FailedToLaunchInstanceDangerNotificationTitle,
                                            thumbnail: ThumbnailHelper.ForInstance(key));
        }
    }

    [RelayCommand]
    private void Deploy(string key)
    {
        try
        {
            _instanceService.Deploy(key);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex,
                                            Resources.Shared_FailedToDeployInstanceDangerNotificationTitle,
                                            thumbnail: ThumbnailHelper.ForInstance(key));
        }
    }

    [RelayCommand]
    private Task OpenFolder(string? key)
    {
        if (key != null)
        {
            var dir = PathDef.Default.DirectoryOfHome(key);
            return TopLevelHelper.LaunchDirectoryInfoAsync(TopLevelHelper.GetTopLevel(),
                                                           new(dir),
                                                           Resources
                                                              .Shared_FailedToOpenInstanceFolderDangerNotificationTitle,
                                                           _notificationService,
                                                           thumbnail: ThumbnailHelper.ForInstance(key));
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void GotoProperties(string? key)
    {
        if (key != null)
        {
            _navigationService.Navigate<InstancePage>(new InstancePageModel.CompositeParameter(key,
                                                          typeof(InstancePropertiesPage)));
        }
    }

    [RelayCommand]
    private void GotoSetup(string? key)
    {
        if (key != null)
        {
            _navigationService.Navigate<InstancePage>(new InstancePageModel.CompositeParameter(key,
                                                          typeof(InstanceSetupPage)));
        }
    }

    [RelayCommand]
    private void OpenNotificationSidebar() => _overlayService.PopSidebar<NotificationSidebar>();

    private bool CanOpenUpdateModal(AppUpdateModel? model) => model != null;

    [RelayCommand(CanExecute = nameof(CanOpenUpdateModal))]
    private void OpenUpdateModal(AppUpdateModel? model)
    {
        if (model == null)
        {
            return;
        }

        _overlayService.PopModal(new AppUpdateModal
        {
            Model = model,
            NotificationService = _notificationService,
            UpdateManager = _updateManager,
        });
    }

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
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex, Resources.MainWindow_CheckForUpdatesDangerNotificationTitle);
        }

        CheckForUpdatesCommand.NotifyCanExecuteChanged();
    }

    private bool CanCheckForUpdates => _updateService.CanCheckUpdate;

    [RelayCommand]
    private async Task OpenGitHubAsync()
    {
        var topLevel = TopLevel.GetTopLevel(
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
        );
        await TopLevelHelper.LaunchUriAsync(
            topLevel,
            new Uri(Program.RepositoryUrl),
            Resources.MainWindow_OpenGitHubDangerNotificationTitle
        );
    }

    #endregion

    #region Other reactive

    private void OnUpdateFound(AppUpdateModel? model)
    {
        Dispatcher.UIThread.Post(() => CurrentUpdate = model);
    }

    // NotificationService 的事件假定在 UI 线程触发（见服务注释），此处直接赋值即可
    private void OnUnreadCountChanged(int count)
    {
        UnreadNotificationCount = count;
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
            InstanceEntryModel model = new(key, item.Name, item.Setup.Version, item.Setup.Loader, item.Setup.Source)
            {
                LastPlayedAtRaw =
                    DateTimeHelper.FromPersistedLocalDateTime(_persistenceService.GetLastActivity(key)?.End),
            };
            list.Add(model);
        }

        _entries.AddOrUpdate(list);
    }

    private void OnProfileAdded(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        // NOTE: 事件有可能在其他线程触发，不过 ModelBase 好像天生有跨线程操作的神力
        var exist = _entries.Items.FirstOrDefault(x => x.Basic.Key == e.Key);
        if (exist != null)
        {
            // Import
            exist.Basic.Name = e.Value.Name;
            exist.Basic.Source = e.Value.Setup.Source;
            exist.Basic.Version = e.Value.Setup.Version;
            exist.Basic.Loader = e.Value.Setup.Loader;
            exist.Basic.UpdateIcon();
        }
        else
        {
            // Install
            exist = new(e.Key, e.Value.Name, e.Value.Setup.Version, e.Value.Setup.Loader, e.Value.Setup.Source);
            _entries.AddOrUpdate(exist);
        }

        // 把以下代码放在这里并不合理，但也没其他地方可以放
        var defaultAccount = _persistenceService.GetDefaultAccount();
        if (defaultAccount != null)
        {
            var cooked = AccountHelper.ToCooked(defaultAccount);
            _persistenceService.SetAccountSelector(e.Key, cooked.Uuid);
        }
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        // NOTE: 事件有可能在其他线程触发，不过 ModelBase 好像天生有跨线程操作的神力
        var model = _entries.Items.FirstOrDefault(x => x.Basic.Key == e.Key);
        if (model is not null)
        {
            model.Basic.Name = e.Value.Name;
            model.Basic.Source = e.Value.Setup.Source;
            model.Basic.Version = e.Value.Setup.Version;
            model.Basic.Loader = e.Value.Setup.Loader;
            model.Basic.UpdateIcon();
        }
    }

    private void OnProfileRemoved(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        // NOTE: 事件有可能在其他线程触发，不过 ModelBase 好像天生有跨线程操作的神力
        var model = _entries.Items.FirstOrDefault(x => x.Basic.Key == e.Key);
        if (model is not null)
        {
            _entries.Remove(model);
        }
    }

    #endregion

    #region Instance State

    internal void SubscribeState(InstanceStateAggregator aggregator)
    {
        aggregator.StateChangeStream
                  .Subscribe(change =>
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
    }

    private void HandleSnapshotUpdate(InstanceStateSnapshot snapshot)
    {
        var model = _entries.Items.FirstOrDefault(x => x.Basic.Key == snapshot.Key);
        if (model is not null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                model.State = snapshot.State;
                model.IsPending = snapshot.Progress is TrackerProgress.Indeterminate;
                model.Progress = snapshot.Progress is TrackerProgress.Determinate d ? d.Percent : 0d;
            });
        }
        else if (snapshot.State == InstanceState.Installing)
        {
            // Install 创建的新实例不在列表中，需要添加
            var newModel = new InstanceEntryModel(snapshot.Key, snapshot.Key, "N/A", null, null);
            Dispatcher.UIThread.Post(() =>
            {
                newModel.State = snapshot.State;
                newModel.IsPending = true;
                newModel.Progress = 0d;
                _entries.AddOrUpdate(newModel);
            });
        }
    }

    private void HandleSnapshotRemove(InstanceStateSnapshot snapshot)
    {
        var model = _entries.Items.FirstOrDefault(x => x.Basic.Key == snapshot.Key);
        if (model is not null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                model.State = InstanceState.Idle;
                model.IsPending = false;
                // Install 完成后移除临时占位条目。ProfileAdded 可能在 tracker
                // 完成前已经填充了真实数据 — 此时保留条目，供后续 Deploy 阶段接管。
                if (snapshot.Tracker is InstallTracker && snapshot.Tracker.State is TrackerState.Finished or TrackerState.Faulted
                    && model.Basic.Source is null)
                {
                    _entries.Remove(model);
                }
            });
        }
    }

    #endregion
}
