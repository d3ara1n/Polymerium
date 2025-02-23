using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Models;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.Tasks;

namespace Polymerium.App;

public partial class MainWindow : AppWindow
{
    private Action<Type, object?, IPageTransition?>? _navigate;

    public static readonly DirectProperty<MainWindow, string> FilterTextProperty = AvaloniaProperty.RegisterDirect<MainWindow, string>(nameof(FilterText), o => o.FilterText, (o, v) => o.FilterText = v);

    private string _filterText = string.Empty;

    public string FilterText
    {
        get => _filterText;
        set => SetAndRaise(FilterTextProperty, ref _filterText, value);
    }


    public MainWindow()
    {
        InitializeComponent();

        ViewInstanceCommand = new RelayCommand<string>(ViewInstance);

        #region Setup Entries View

        var filter = this.GetObservable(FilterTextProperty).Select(BuildFilter);
        _subscription = _entries.Connect().Filter(filter).Bind(out var view).Subscribe();
        View = view;

        #endregion
    }

    private readonly SourceCache<InstanceEntryModel, string> _entries = new(x => x.Basic.Key);
    private readonly IDisposable _subscription;

    public Frame.PageActivatorDelegate PageActivator { get; private set; }

    public static readonly DirectProperty<MainWindow, ReadOnlyObservableCollection<InstanceEntryModel>> ViewProperty = AvaloniaProperty.RegisterDirect<MainWindow, ReadOnlyObservableCollection<InstanceEntryModel>>(nameof(View), o => o.View, (o, v) => o.View = v);

    private ReadOnlyObservableCollection<InstanceEntryModel> _view;

    public ReadOnlyObservableCollection<InstanceEntryModel> View
    {
        get => _view;
        set => SetAndRaise(ViewProperty, ref _view, value);
    }

    public ICommand ViewInstanceCommand { get; }

    private static Func<InstanceEntryModel, bool> BuildFilter(string filter) => x => string.IsNullOrEmpty(filter) || x.Basic.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private void PopDialog()
    {
        Button pop = new() { Content = "POP" };
        pop.Click += (_, __) => PopDialog();
        PopDialog(new Dialog { Title = $"DIALOG {Random.Shared.Next(1000, 9999)}", Message = "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM", Content = new StackPanel { Spacing = 8d, Children = { new TextBox(), pop } } });
    }

    private void PopToast()
    {
        Button pop = new() { Content = "POP" };
        pop.Click += (_, __) => PopToast();
        PopToast(new Toast { Title = $"A VERY LONG TOAST TITLE {Random.Shared.Next(1000, 9999)}", Content = new StackPanel { Spacing = 8d, Children = { new TextBlock { Text = "ALIVE OR DEAD VERY LONG MESSAGE THAT DONT TRIM" }, new TextBox(), pop } } });
    }

    private void PopNotification()
    {
        NotificationItem item = new() { Content = "Larry The Lazy" };
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
            PopDialog();
        }
        else
        {
            (Type Page, object? Parameter) target = sender switch
            {
                Button { Tag: "ExhibitionView" } => (typeof(ExhibitionView), null),
                Button { Tag: "UnknownView" } => (typeof(UnknownView), Random.Shared.Next(1000, 9999)),
                Button { Tag: "CreateInstanceView" } => (typeof(NewInstanceView), null),
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

    internal void BindNavigation(Action<Type, object?, IPageTransition?> navigate, Frame.PageActivatorDelegate activator)
    {
        _navigate = navigate;
        PageActivator = activator;
        Root.PageActivator = activator;
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
            InstanceEntryModel model = new(key, item.Name, item.Setup.Version, item.Setup.Loader, item.Setup.Source);
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
            exist.Basic.Name = e.Value.Name;
            exist.Basic.Source = e.Value.Setup.Source;
            exist.Basic.Version = e.Value.Setup.Version;
            exist.Basic.Loader = e.Value.Setup.Loader;
            exist.Basic.UpdateIcon();
        }
        else
        {
            InstanceEntryModel model = new(e.Key, e.Value.Name, e.Value.Setup.Version, e.Value.Setup.Loader, e.Value.Setup.Source);
            _entries.AddOrUpdate(model);
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
            _entries.Remove(model);
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
        var model = _entries.Items.FirstOrDefault(x => x.Basic.Key == e.Key);
        if (model is null)
            return;

        model.State = InstanceEntryState.Updating;

        var progressUpdater = Observable.Interval(TimeSpan.FromMilliseconds(1000)).Select(x => model.Progress = e.Progress).Subscribe();

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
                    Dispatcher.UIThread.Post(() => PopNotification(new NotificationItem { Content = e.FailureReason, Title = $"Failed to update {e.Key}", Level = NotificationLevel.Warning }));
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
        InstanceEntryModel model = new(e.Key, e.Key, "Unknown", null, null) { State = InstanceEntryState.Installing };
        var progressUpdater = Observable.Interval(TimeSpan.FromMilliseconds(1000)).Select(_ => model.Progress = e.Progress).Subscribe();

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
                    Dispatcher.UIThread.Post(() => PopNotification(new NotificationItem { Content = e.FailureReason, Title = $"Failed to install {e.Key}", Level = NotificationLevel.Danger }));
                    _entries.Remove(model);
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
        _entries.AddOrUpdate(model);
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