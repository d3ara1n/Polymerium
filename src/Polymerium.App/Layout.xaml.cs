using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App;

public sealed partial class Layout
{
    public static readonly DependencyProperty OverlayProperty =
        DependencyProperty.Register(nameof(Overlay), typeof(ModalBase), typeof(Layout), new PropertyMetadata(null));

    private Action<Type, object?, NavigationTransitionInfo>? navigateHandler;
    private int runningTaskCount;

    public Layout()
    {
        InitializeComponent();
        DismissModalCommand = new RelayCommand(OnDismissModal);
    }


    public ModalBase? Overlay
    {
        get => (ModalBase?)GetValue(OverlayProperty);
        set => SetValue(OverlayProperty, value);
    }

    public ObservableCollection<InfoBar> Notifications { get; } = new();
    public ICommand DismissModalCommand { get; }

    public Border Titlebar => AppTitleBar;

    public bool CanGoBack => Root.CanGoBack;
    public void GoBack() => Root.GoBack();

    public void OnActivate(bool activate) =>
        VisualStateManager.GoToState(this, activate ? "Activated" : "Deactivated", true);

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

    public void OnEnqueueNotification(NotificationItem item) =>
        DispatcherQueue.TryEnqueue(() =>
        {
            var bar = new InfoBar
            {
                Message = item.Message,
                IsOpen = true,
                IsClosable = true,
                Severity = item.Severity,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = (Brush)App.Current.Resources["AcrylicInAppFillColorDefaultBrush"]
            };
            bar.Closed += InfoBar_Closed;
            if (item.Severity != InfoBarSeverity.Error && item.Severity != InfoBarSeverity.Warning)
            {
                var fadeAnimation = new DoubleAnimation { BeginTime = TimeSpan.FromSeconds(5) };
                fadeAnimation.From = 1d;
                fadeAnimation.To = 0d;
                fadeAnimation.Duration = TimeSpan.FromMilliseconds(250);
                fadeAnimation.EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut };
                var storyboard = new Storyboard();
                storyboard.Children.Add(fadeAnimation);
                storyboard.Completed += (s, e) =>
                {
                    bar.IsOpen = false;
                };
                Storyboard.SetTarget(storyboard, bar);
                Storyboard.SetTargetProperty(storyboard, "Opacity");
                Notifications.Add(bar);
                storyboard.Begin();
            }
            else
            {
                Notifications.Add(bar);
            }
        });

    public void OnPopModal(ModalBase modal) =>
        DispatcherQueue.TryEnqueue(() =>
        {
            modal.XamlRoot = XamlRoot;
            modal.DismissCommand = DismissModalCommand;
            Overlay = modal;
            VisualStateManager.GoToState(this, "Shown", true);
        });

    public void OnDismissModal() => VisualStateManager.GoToState(this, "Hidden", true);

    public void SetHandler(Action<Type, object?, NavigationTransitionInfo> handler) => navigateHandler = handler;


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

    private void NavigationViewControl_BackRequested(NavigationView _, NavigationViewBackRequestedEventArgs __) =>
        Root.GoBack();

    private void HiddenStoryboard_Completed(object sender, object e) => Overlay = null;

    private void InfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args) => Notifications.Remove(sender);

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems.First();
        navigateHandler?.Invoke(typeof(HomeView), null, new SuppressNavigationTransitionInfo());
    }

    private void NavigationViewControl_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is { Tag: string tag })
        {
            navigateHandler?.Invoke(tag?.ToUpper() switch
            {
                "HOME" => typeof(HomeView),
                "INSTANCES" => typeof(DesktopView),
                "ACCOUNTS" => typeof(AccountView),
                "MARKET" => typeof(MarketView),
                "TOOLBOX" => typeof(ToolboxView),
                "SETTINGS" => typeof(SettingView),
                "TASKS" => typeof(TaskView),
                _ => typeof(NotFoundView)
            }, null, args.RecommendedNavigationTransitionInfo);
        }
    }
}