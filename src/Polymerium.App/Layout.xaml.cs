using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App
{
    public sealed partial class Layout : UserControl
    {
        public Border Titlebar => AppTitleBar;
        public Layout()
        {
            this.InitializeComponent();
        }

        private Action<Type, NavigationTransitionInfo>? selectHandler;

        public void OnActivate(bool activate) => VisualStateManager.GoToState(this, activate ? "Activated" : "Deactivated", true);

        public void OnNavigate(Type view, object? parameter, NavigationTransitionInfo? info, bool isRoot)
        {
            Root.Navigate(view, parameter, info);
            if (isRoot) Root.BackStack.Clear();
        }
        public void SetMainMenu(IEnumerable<NavItem> menu)
        {
            NavigationViewControl.MenuItemsSource = menu;
            NavigationViewControl.SelectedItem = menu.First();
        }
        public void SetSideMenu(IEnumerable<NavItem> menu) => NavigationViewControl.FooterMenuItemsSource = menu;
        public void SetHandler(Action<Type, NavigationTransitionInfo> handler) => selectHandler = handler;


        private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
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
                    _ => "Default"
                }, true);
            }
        }

        private void NavigationViewControl_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var item = args.SelectedItem as NavItem;
            if (item != null) selectHandler?.Invoke(item.View, args.RecommendedNavigationTransitionInfo);
        }

        private void NavigationViewControl_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            Root.GoBack();
        }
    }
}
