using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Trident.Abstractions.Tasks;

namespace Polymerium.App;

public partial class MainWindow : AppWindow
{
    private Action<Type, object?, IPageTransition?>? _navigate;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        ViewInstanceCommand = new RelayCommand<string>(ViewInstance);
    }

    public AvaloniaList<InstanceEntryModel> Entries { get; } = [];

    public ICommand ViewInstanceCommand { get; }

    private void PopDialog()
    {
        var pop = new Button { Content = "POP" };
        pop.Click += (_, __) => PopDialog();
        PopDialog(new Dialog
        {
            Title = $"DIALOG {Random.Shared.Next(1000, 9999)}",
            Message = "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM",
            Content = new StackPanel { Spacing = 8d, Children = { new TextBox(), pop } }
        });
    }

    private void PopToast()
    {
        var pop = new Button { Content = "POP" };
        pop.Click += (_, __) => PopToast();
        PopToast(new Toast
        {
            Title = $"A VERY LONG TOAST TITLE {Random.Shared.Next(1000, 9999)}",
            Content = new StackPanel
            {
                Spacing = 8d,
                Children =
                {
                    new TextBlock { Text = "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM" },
                    new TextBox(),
                    pop
                }
            }
        });
    }

    private void PopNotification()
    {
        var item = new NotificationItem { Content = "Larry The Lazy" };
        PopNotification(item);
    }

    private void NavigationButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: "42" })
        {
            PopToast();
        }
        else if (sender is Button { Tag: "0721" })
        {
            PopModal(new AccountPickerModal());
        }
        else
        {
            (Type Page, object? Parameter) target = sender switch
            {
                Button { Tag: "ExhibitionView" } => (typeof(ExhibitionView), null),
                Button { Tag: "UnknownView" } => (typeof(UnknownView), Random.Shared.Next(1000, 9999)),
                Button { Tag: "CreateInstanceView" } => (typeof(CreateInstanceView), null),
                _ => (typeof(PageNotReachedView), null)
            };
            _navigate?.Invoke(target.Page, target.Parameter, null);
        }
    }

    private void ViewInstance(string? key)
    {
        if (key is not null)
            _navigate?.Invoke(typeof(InstanceView), key, null);
    }

    #region Navigation Service

    internal void Navigate(Type page, object? parameter, IPageTransition transition) =>
        // NavigationService 会处理错误情况
        Root.Navigate(page, parameter, transition);

    internal void BindNavigation(Action<Type, object?, IPageTransition?> navigate,
        Frame.PageActivatorDelegate activator)
    {
        _navigate = navigate;
        Root.PageActivator = activator;
    }

    #endregion

    #region Profile Service

    internal void SubscribeProfileList(ProfileManager manager)
    {
        manager.ProfileAdded += OnProfileAdded;
        manager.ProfileUpdated += OnProfileUpdated;
        manager.ProfileRemoved += OnProfileRemoved;

        foreach (var (key, item) in manager.Profiles)
        {
            var model = new InstanceEntryModel(key, item.Name, item.Setup.Version, item.Setup.Loader,
                item.Setup.Source);
            Entries.Add(model);
        }
    }

    private void OnProfileAdded(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        // NOTE: 事件有可能在其他线程触发，不过 ModelBase 好像天生有跨线程操作的神力
        var exist = Entries.FirstOrDefault(x => x.Basic.Key == e.Key);
        if (exist != null)
        {
            exist.Basic.Name = e.Value.Name;
            exist.Basic.Source = e.Value.Setup.Source;
            exist.Basic.Version = e.Value.Setup.Version;
            exist.Basic.Loader = e.Value.Setup.Loader;
            exist.Basic.UpdateIcon();
        }
        else
        {
            var model = new InstanceEntryModel(e.Key, e.Value.Name, e.Value.Setup.Version, e.Value.Setup.Loader,
                e.Value.Setup.Source);
            Entries.Add(model);
        }
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        // NOTE: 事件有可能在其他线程触发，不过 ModelBase 好像天生有跨线程操作的神力
        var model = Entries.FirstOrDefault(x => x.Basic.Key == e.Key);
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
        var model = Entries.FirstOrDefault(x => x.Basic.Key == e.Key);
        if (model is not null) Entries.Remove(model);
    }

    #endregion

    #region State Service

    internal void SubscribeState(InstanceManager manager)
    {
        manager.InstanceInstalling += OnInstanceInstalling;
        manager.InstanceUpdating += OnInstanceUpdating;
    }

    private void OnInstanceUpdating(object? sender, UpdateTracker e)
    {
        // NOTE: 事件有可能在其他线程触发，不过 ModelBase 好像天生有跨线程操作的神力
        var model = Entries.FirstOrDefault(x => x.Basic.Key == e.Key);
        if (model is null) return;

        model.State = InstanceEntryState.Updating;

        var progressUpdater = Observable.Interval(TimeSpan.FromMilliseconds(1000))
            .Select(x => model.Progress = e.Progress).Subscribe();

        void OnStateChanged(TrackerBase _, TrackerState state)
        {
            switch (state)
            {
                case TrackerState.Idle:
                    break;
                case TrackerState.Running:
                    model.Progress = null;
                    break;
                case TrackerState.Faulted:
                    Dispatcher.UIThread.Post(() => PopNotification(new NotificationItem
                    {
                        Content = e.FailureReason,
                        Title = $"Failed to update {e.Key}",
                        Level = NotificationLevel.Danger
                    }));
                    progressUpdater.Dispose();
                    e.StateUpdated -= OnStateChanged;
                    break;
                case TrackerState.Finished:
                    model.State = InstanceEntryState.Idle;
                    progressUpdater.Dispose();
                    e.StateUpdated -= OnStateChanged;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        e.StateUpdated += OnStateChanged;
    }

    private void OnInstanceInstalling(object? sender, InstallTracker e)
    {
        // NOTE: 事件有可能在其他线程触发，不过 ModelBase 好像天生有跨线程操作的神力
        var model = new InstanceEntryModel(e.Key, e.Key, "Unknown", null, null)
        {
            State = InstanceEntryState.Installing
        };
        var progressUpdater = Observable.Interval(TimeSpan.FromMilliseconds(1000))
            .Select(_ => model.Progress = e.Progress).Subscribe();

        void OnStateChanged(TrackerBase _, TrackerState state)
        {
            switch (state)
            {
                case TrackerState.Idle:
                    break;
                case TrackerState.Running:
                    model.Progress = null;
                    break;
                case TrackerState.Faulted:
                    Dispatcher.UIThread.Post(() => PopNotification(new NotificationItem
                    {
                        Content = e.FailureReason,
                        Title = $"Failed to install {e.Key}",
                        Level = NotificationLevel.Danger
                    }));
                    Entries.Remove(model);
                    progressUpdater.Dispose();
                    e.StateUpdated -= OnStateChanged;
                    break;
                case TrackerState.Finished:
                    model.State = InstanceEntryState.Idle;
                    progressUpdater.Dispose();
                    e.StateUpdated -= OnStateChanged;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        e.StateUpdated += OnStateChanged;
        Entries.Add(model);
    }

    #endregion

    #region Window State Management

    private void ToggleMaximize()
    {
        switch (WindowState)
        {
            case WindowState.Normal:
                WindowState = WindowState.Maximized;
                break;
            case WindowState.Maximized:
                WindowState = WindowState.Normal;
                break;
        }
    }

    private void MinimizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
        e.Handled = true;
    }

    private void ToggleMaximizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ToggleMaximize();
        e.Handled = true;
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
        e.Handled = true;
    }

    #endregion
}