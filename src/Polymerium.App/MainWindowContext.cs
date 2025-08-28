using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Models;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Properties;
using Polymerium.App.Services;
using Polymerium.App.Utilities;
using Polymerium.App.ViewModels;
using Polymerium.App.Views;
using Trident.Core.Exceptions;
using Trident.Core.Igniters;
using Trident.Core.Services;
using Trident.Core.Services.Instances;
using Trident.Abstractions;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.Tasks;

namespace Polymerium.App
{
    public partial class MainWindowContext : ObservableObject
    {
        #region Fields

        private readonly SourceCache<InstanceEntryModel, string> _entries = new(x => x.Basic.Key);

        #endregion

        public MainWindowContext(
            ProfileManager profileManager,
            InstanceManager instanceManager,
            NotificationService notificationService,
            NavigationService navigationService,
            PersistenceService persistenceService,
            InstanceService instanceService,
            OverlayService overlayService)
        {
            _notificationService = notificationService;
            _navigationService = navigationService;
            _persistenceService = persistenceService;
            _instanceService = instanceService;
            _overlayService = overlayService;

            SubscribeProfileList(profileManager);
            SubscribeState(instanceManager);

            var filter = this.WhenValueChanged(x => x.FilterText).Select(BuildFilter);
            _ = _entries
               .Connect()
               .Filter(filter)
               .SortAndBind(out var view,
                            SortExpressionComparer<InstanceEntryModel>.Descending(x => x.LastPlayedAtRaw
                             ?? DateTimeOffset.MinValue))
               .Subscribe();
            View = view;

            if (OperatingSystem.IsWindows() && (Program.Debug || Program.FirstRun))
            {
                var model = new PrivilegeRequirementModal { NotificationService = notificationService };
                if (!model.Check())
                {
                    _overlayService.PopModal(model);
                }
            }
        }


        private static Func<InstanceEntryModel, bool> BuildFilter(string? filter) =>
            x => string.IsNullOrEmpty(filter) || x.Basic.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);

        #region Injected

        private readonly NotificationService _notificationService;
        private readonly NavigationService _navigationService;
        private readonly PersistenceService _persistenceService;
        private readonly InstanceService _instanceService;
        private readonly OverlayService _overlayService;

        #endregion

        #region Reactive

        [ObservableProperty]
        public partial ReadOnlyObservableCollection<InstanceEntryModel> View { get; set; }

        [ObservableProperty]
        public partial string FilterText { get; set; } = string.Empty;

        #endregion

        #region Commands

        [RelayCommand]
        private void ViewInstance(string? key)
        {
            if (key is not null)
            {
                _navigationService.Navigate<InstanceView>(key);
            }
        }

        [RelayCommand]
        private void Navigate(Type? page)
        {
            if (page != null)
            {
                _navigationService.Navigate(page);
            }
        }

        [RelayCommand]
        private void ViewLog(LaunchTracker? tracker)
        {
            if (tracker != null)
            {
                var path = Path.Combine(PathDef.Default.DirectoryOfBuild(tracker.Key), "logs", "latest.log");
                if (File.Exists(path))
                {
                    TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchFileInfoAsync(new(path));
                }
                else
                {
                    _notificationService.PopMessage("Log file not found",
                                                    "Failed to open log file",
                                                    NotificationLevel.Warning);
                }
            }
        }

        [RelayCommand]
        private async Task PlayAsync(string key) =>
            await _instanceService.DeployAndLaunchAsync(key, LaunchMode.Managed);

        [RelayCommand]
        private void Deploy(string key) => _instanceService.Deploy(key);

        [RelayCommand]
        private void OpenFolder(string? key)
        {
            if (key != null)
            {
                var dir = PathDef.Default.DirectoryOfHome(key);
                TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(dir));
            }
        }

        [RelayCommand]
        private void GotoProperties(string? key)
        {
            if (key != null)
            {
                _navigationService.Navigate<InstanceView>(new InstanceViewModel.CompositeParameter(key,
                                                              typeof(InstancePropertiesView)));
            }
        }

        [RelayCommand]
        private void GotoSetup(string? key)
        {
            if (key != null)
            {
                _navigationService.Navigate<InstanceView>(new InstanceViewModel.CompositeParameter(key,
                                                              typeof(InstanceSetupView)));
            }
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
                InstanceEntryModel model = new(key,
                                               item.Name,
                                               item.Setup.Version,
                                               item.Setup.Loader,
                                               item.Setup.Source);
                model.LastPlayedAtRaw = _persistenceService.GetLastActivity(key)?.End;
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

        #region State Service

        internal void SubscribeState(InstanceManager manager)
        {
            manager.InstanceUpdating += OnInstanceUpdating;
            manager.InstanceInstalling += OnInstanceInstalling;
            manager.InstanceDeploying += OnInstanceDeploying;
            manager.InstanceLaunching += OnInstanceLaunching;
        }

        private void OnInstanceInstalling(object? sender, InstallTracker e)
        {
            // NOTE: 事件有可能在其他线程触发，不过 ModelBase 好像天生有跨线程操作的神力
            var model = new InstanceEntryModel(e.Key, e.Key, "N/A", null, null);

            e
               .ProgressStream.Buffer(TimeSpan.FromSeconds(1))
               .Where(x => x.Any())
               .Select(x => x.Last())
               .Subscribe(x =>
                {
                    model.IsPending = !x.HasValue;
                    model.Progress = x ?? 0d;
                })
               .DisposeWith(e);

            e.StateUpdated += OnStateChanged;
            _entries.AddOrUpdate(model);
            return;

            void OnStateChanged(TrackerBase _, TrackerState state)
            {
                switch (state)
                {
                    case TrackerState.Idle:
                        break;
                    case TrackerState.Running:
                        model.IsPending = true;
                        model.Progress = 0d;
                        Dispatcher.UIThread.Post(() => model.State = InstanceEntryState.Installing);
                        break;
                    case TrackerState.Faulted when e.FailureReason is not OperationCanceledException:
                        Dispatcher.UIThread.Post(() =>
                        {
                            model.State = InstanceEntryState.Idle;
                            _entries.Remove(model);
                            _notificationService.PopMessage(e.FailureReason,
                                                            Resources
                                                               .MainWindow_InstanceInstallingDangerNotificationTitle
                                                               .Replace("{0}", e.Key));
                        });
                        e.StateUpdated -= OnStateChanged;
                        break;
                    case TrackerState.Finished:
                        Dispatcher.UIThread.Post(() =>
                        {
                            model.State = InstanceEntryState.Idle;
                            _notificationService.PopMessage(Resources
                                                               .MainWindow_InstanceInstallingSuccessNotificationPrompt,
                                                            e.Key,
                                                            NotificationLevel.Success,
                                                            actions: new NotificationAction(Resources
                                                                   .MainWindow_InstanceInstallingSuccessNotificationOpenText,
                                                                ViewInstanceCommand,
                                                                e.Key),
                                                            forceExpire: true);
                        });
                        _persistenceService.AppendAction(new(e.Key,
                                                             PersistenceService.ActionKind.Install,
                                                             null,
                                                             e.Source));
                        e.StateUpdated -= OnStateChanged;
                        break;
                    case TrackerState.Faulted when e.FailureReason is OperationCanceledException:
                        Dispatcher.UIThread.Post(() =>
                        {
                            model.State = InstanceEntryState.Idle;
                            _entries.Remove(model);
                        });
                        e.StateUpdated -= OnStateChanged;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            }
        }

        private void OnInstanceUpdating(object? sender, UpdateTracker e)
        {
            // NOTE: 事件有可能在其他线程触发，不过 ModelBase 好像天生有跨线程操作的神力
            var model = _entries.Items.FirstOrDefault(x => x.Basic.Key == e.Key);
            if (model is null)
            {
                return;
            }

            e
               .ProgressStream.Buffer(TimeSpan.FromSeconds(1))
               .Where(x => x.Any())
               .Select(x => x.Last())
               .Subscribe(x =>
                {
                    model.IsPending = !x.HasValue;
                    model.Progress = x ?? 0d;
                })
               .DisposeWith(e);

            e.StateUpdated += OnStateChanged;
            return;

            void OnStateChanged(TrackerBase _, TrackerState state)
            {
                switch (state)
                {
                    case TrackerState.Idle:
                        break;
                    case TrackerState.Running:
                        model.IsPending = true;
                        model.Progress = 0d;
                        Dispatcher.UIThread.Post(() => model.State = InstanceEntryState.Updating);
                        break;
                    case TrackerState.Faulted when e.FailureReason is not OperationCanceledException:
                        Dispatcher.UIThread.Post(() =>
                        {
                            model.State = InstanceEntryState.Idle;
                            _notificationService.PopMessage(e.FailureReason,
                                                            Resources.MainWindow_InstanceUpdatingDangerNotificationTitle
                                                                     .Replace("{0}", e.Key));
                        });
                        e.StateUpdated -= OnStateChanged;
                        break;
                    case TrackerState.Finished:
                        Dispatcher.UIThread.Post(() =>
                        {
                            model.State = InstanceEntryState.Idle;
                            _notificationService.PopMessage(Resources
                                                               .MainWindow_InstanceUpdatingSuccessNotificationPrompt,
                                                            e.Key,
                                                            NotificationLevel.Success,
                                                            true,
                                                            new NotificationAction(Resources
                                                                   .MainWindow_InstanceUpdatingSuccessNotificationOpenText,
                                                                ViewInstanceCommand,
                                                                e.Key));
                        });
                        _persistenceService.AppendAction(new(e.Key,
                                                             PersistenceService.ActionKind.Update,
                                                             e.OldSource,
                                                             e.NewSource));
                        e.StateUpdated -= OnStateChanged;
                        break;
                    case TrackerState.Faulted when e.FailureReason is OperationCanceledException:
                        Dispatcher.UIThread.Post(() => model.State = InstanceEntryState.Idle);
                        e.StateUpdated -= OnStateChanged;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            }
        }

        private void OnInstanceDeploying(object? sender, DeployTracker e)
        {
            var model = _entries.Items.FirstOrDefault(x => x.Basic.Key == e.Key);
            if (model is null)
            {
                return;
            }

            e
               .StageStream.Subscribe(x =>
                {
                    model.IsPending = true;
                    model.Progress = 0d;
                })
               .DisposeWith(e);
            e
               .ProgressStream.Buffer(TimeSpan.FromSeconds(1))
               .Where(x => x.Any())
               .Select(x => x.Last())
               .Subscribe(x =>
                {
                    model.IsPending = false;
                    model.Progress = x.Item2 != 0 ? x.Item1 * 100d / x.Item2 : 0;
                })
               .DisposeWith(e);

            e.StateUpdated += OnStateChanged;
            return;

            void OnStateChanged(TrackerBase _, TrackerState state)
            {
                switch (state)
                {
                    case TrackerState.Idle:
                        break;
                    case TrackerState.Running:
                        model.IsPending = true;
                        model.Progress = 0d;
                        Dispatcher.UIThread.Post(() => model.State = InstanceEntryState.Preparing);
                        break;
                    case TrackerState.Faulted when e.FailureReason is not OperationCanceledException:
                        Dispatcher.UIThread.Post(() =>
                        {
                            model.State = InstanceEntryState.Idle;
                            _notificationService.PopMessage(e.FailureReason,
                                                            Resources.MainWindow_InstanceDeployingNotificationTitle
                                                                     .Replace("{0}", e.Key));
                        });
                        e.StateUpdated -= OnStateChanged;
                        break;
                    case TrackerState.Finished:
                        Dispatcher.UIThread.Post(() =>
                        {
                            model.State = InstanceEntryState.Idle;
                            _notificationService.PopMessage(Resources
                                                               .MainWindow_InstanceDeployingSuccessNotificationPrompt,
                                                            e.Key,
                                                            NotificationLevel.Success);
                        });
                        e.StateUpdated -= OnStateChanged;
                        break;
                    case TrackerState.Faulted when e.FailureReason is OperationCanceledException:
                        Dispatcher.UIThread.Post(() => model.State = InstanceEntryState.Idle);
                        e.StateUpdated -= OnStateChanged;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            }
        }

        private void OnInstanceLaunching(object? sender, LaunchTracker e)
        {
            // NOTE: 事件有可能在其他线程触发，不过 ModelBase 好像天生有跨线程操作的神力
            var model = _entries.Items.FirstOrDefault(x => x.Basic.Key == e.Key);
            if (model is null)
            {
                return;
            }

            e.StateUpdated += OnStateChanged;
            return;

            void OnStateChanged(TrackerBase _, TrackerState state)
            {
                switch (state)
                {
                    case TrackerState.Idle:
                        break;
                    case TrackerState.Running:
                        model.IsPending = true;
                        model.Progress = 0d;
                        Dispatcher.UIThread.Post(() =>
                        {
                            // 不从 ProfileManager 里取，反正 UI 上只要行为类似就好
                            model.LastPlayedAtRaw = DateTimeOffset.Now;
                            model.State = InstanceEntryState.Running;
                        });
                        break;
                    case TrackerState.Faulted when e.FailureReason is not OperationCanceledException:
                        Dispatcher.UIThread.Post(() =>
                        {
                            model.State = InstanceEntryState.Idle;
                            if (e.FailureReason is AggregateException { InnerException: ProcessFaultedException }
                                                or ProcessFaultedException)
                            {
                                _notificationService.PopMessage(e.FailureReason,
                                                                e.Key,
                                                                actions:
                                                                [
                                                                    new(Resources
                                                                           .MainWindow_InstanceLaunchingDangerNotificationViewOutputText,
                                                                        ViewLogCommand,
                                                                        e)
                                                                ]);
                            }
                            else
                            {
                                _notificationService.PopMessage(e.FailureReason, e.Key);
                            }
                        });
                        _persistenceService.AppendActivity(new(e.Key, e.StartedAt, DateTimeOffset.Now, false));
                        e.StateUpdated -= OnStateChanged;
                        break;
                    case TrackerState.Finished:
                        Dispatcher.UIThread.Post(() =>
                        {
                            model.State = InstanceEntryState.Idle;
                            _notificationService.PopMessage(Resources
                                                               .MainWindow_InstanceLaunchingSuccessNotificationPrompt,
                                                            e.Key,
                                                            NotificationLevel.Success);
                        });
                        _persistenceService.AppendActivity(new(e.Key, e.StartedAt, DateTimeOffset.Now, true));
                        e.StateUpdated -= OnStateChanged;
                        break;
                    case TrackerState.Faulted when e.FailureReason is OperationCanceledException:
                        Dispatcher.UIThread.Post(() => model.State = InstanceEntryState.Idle);
                        e.StateUpdated -= OnStateChanged;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
            }
        }

        #endregion
    }
}
