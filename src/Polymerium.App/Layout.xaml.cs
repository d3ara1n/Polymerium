using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Extensions;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Tasks;
using Polymerium.App.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Trident.Abstractions.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App
{
    public sealed partial class Layout
    {
        public static readonly DependencyProperty OverlayProperty =
            DependencyProperty.Register(nameof(Overlay), typeof(ModalBase), typeof(Layout), new PropertyMetadata(null));

        private Action<Type, object?, NavigationTransitionInfo>? navigateHandler;
        private int runningTaskCount;


        public Layout()
        {
            InitializeComponent();

            RunningTaskCount = this.ToBindable(x => x.runningTaskCount, (x, v) => x.runningTaskCount = v);
            AbortTaskCommand = new RelayCommand<TaskBase>(AbortTask);
            ClearTasksCommand = new RelayCommand(ClearTasks);
            DismissModalCommand = new RelayCommand(OnDismissModal);
        }


        public ModalBase? Overlay
        {
            get => (ModalBase?)GetValue(OverlayProperty);
            set => SetValue(OverlayProperty, value);
        }

        public ObservableCollection<TaskModel> Tasks { get; } = new();
        public ObservableCollection<NotificationItem> Notifications { get; } = new();
        public ICommand AbortTaskCommand { get; }
        public ICommand ClearTasksCommand { get; }
        public ICommand DismissModalCommand { get; }
        public Bindable<Layout, int> RunningTaskCount { get; }

        public Border Titlebar => AppTitleBar;

        public void OnActivate(bool activate)
        {
            VisualStateManager.GoToState(this, activate ? "Activated" : "Deactivated", true);
        }

        public void OnNavigate(Type view, object? parameter, NavigationTransitionInfo? info, bool isRoot)
        {
            if (info != null)
            {
                Root.Navigate(view, parameter, info);
            }
            else
            {
                Root.Navigate(view, parameter);
            }

            if (isRoot)
            {
                Root.BackStack.Clear();
            }
        }

        public void OnEnqueueNotification(NotificationItem item)
        {
            DispatcherQueue.TryEnqueue(() => Notifications.Add(item));
        }

        public void OnPopModal(ModalBase modal)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                modal.XamlRoot = XamlRoot;
                modal.DismissCommand = DismissModalCommand;
                Overlay = modal;
                VisualStateManager.GoToState(this, "Shown", true);
            });
        }

        public void OnDismissModal()
        {
            VisualStateManager.GoToState(this, "Hidden", true);
        }


        public void OnEnqueueTask(TaskBase task)
        {
            task.Subscribe(OnTaskUpdate);
            TaskModel models = new(task, DispatcherQueue, AbortTaskCommand);
            DispatcherQueue.TryEnqueue(() => Tasks.Add(models));
        }

        public void SetHandler(Action<Type, object?, NavigationTransitionInfo> handler)
        {
            navigateHandler = handler;
        }


        private void NavigationViewControl_DisplayModeChanged(NavigationView sender,
            NavigationViewDisplayModeChangedEventArgs args)
        {
            if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                VisualStateManager.GoToState(this, "Top", true);
            }
            else
            {
                VisualStateManager.GoToState(this, args.DisplayMode switch
                {
                    NavigationViewDisplayMode.Minimal => "Minimal",
                    NavigationViewDisplayMode.Compact => "Compact",
                    _ => "Normal"
                }, true);
            }
        }

        private void NavigationViewControl_SelectionChanged(NavigationView sender,
            NavigationViewSelectionChangedEventArgs args)
        {
            //if (args.SelectedItem is NavItem item)
            //{
            //    navigateHandler?.Invoke(item.View, null, args.RecommendedNavigationTransitionInfo);
            //}
            if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tag)
            {
                navigateHandler?.Invoke(tag switch
                {
                    "HOME" => typeof(HomeView),
                    "INSTANCES" => typeof(DesktopView),
                    "ACCOUNTS" => typeof(AccountView),
                    "MARKET" => typeof(MarketView),
                    "TOOLBOX" => typeof(ToolboxView),
                    "SETTINGS" => typeof(SettingView),
                    _ => throw new NotImplementedException()
                }, null, args.RecommendedNavigationTransitionInfo); ;
            }
        }

        private void NavigationViewControl_BackRequested(NavigationView _, NavigationViewBackRequestedEventArgs __)
        {
            Root.GoBack();
        }

        private void Hyperlink_OnClick(Hyperlink _, HyperlinkClickEventArgs __)
        {
            FlyoutBase.ShowAttachedFlyout(TaskPanel);
        }

        private void AbortTask(TaskBase? task)
        {
            task?.Abort();
        }

        private void ClearTasks()
        {
            var toClears = Tasks
                .Where(x => x.State.Value != TaskState.Idle && x.State.Value != TaskState.Running)
                .ToArray();
            foreach (var clear in toClears)
            {
                Tasks.Remove(clear);
            }
        }

        private void OnTaskUpdate(TaskBase task, TaskProgressUpdatedEventArgs args)
        {
            if (args.State == task.State)
            {
                return;
            }

            var offset = 0;
            if (args.State == TaskState.Idle)
            {
                // do nothing
            }
            else if (args.State == TaskState.Running)
            {
                offset = +1;
            }
            else
            {
                offset = -1;
            }

            DispatcherQueue.TryEnqueue(() => { RunningTaskCount.Value += offset; });
        }

        private void HiddenStoryboard_Completed(object sender, object e)
        {
            Overlay = null;
        }

        private void InfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
        {
            var raw = sender.Tag as NotificationItem;
            if (raw != null)
            {
                Notifications.Remove(raw);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems.First();
        }
    }
}