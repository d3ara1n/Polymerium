using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App;

public sealed partial class Layout
{
    private Action<Type, object?, NavigationTransitionInfo>? selectHandler;

    public Layout()
    {
        InitializeComponent();
    }

    public Border Titlebar => AppTitleBar;

    public void OnActivate(bool activate)
    {
        VisualStateManager.GoToState(this, activate ? "Activated" : "Deactivated", true);
    }

    public void OnNavigate(Type view, object? parameter, NavigationTransitionInfo? info, bool isRoot)
    {
            if (info != null)
                Root.Navigate(view, parameter, info);
            else
                Root.Navigate(view, parameter);
            if (isRoot) Root.BackStack.Clear();
    }

    public void SetMainMenu(IEnumerable<NavItem> menu)
    {
        NavigationViewControl.MenuItemsSource = menu;
        NavigationViewControl.SelectedItem = menu.First();
    }

    public void SetSideMenu(IEnumerable<NavItem> menu)
    {
        NavigationViewControl.FooterMenuItemsSource = menu;
    }

    public void SetHandler(Action<Type, object?, NavigationTransitionInfo> handler)
    {
        selectHandler = handler;
    }


    private void NavigationViewControl_DisplayModeChanged(NavigationView sender,
        NavigationViewDisplayModeChangedEventArgs args)
    {
        if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            VisualStateManager.GoToState(this, "Top", true);
        else
            VisualStateManager.GoToState(this, args.DisplayMode switch
            {
                NavigationViewDisplayMode.Minimal => "Minimal",
                NavigationViewDisplayMode.Compact => "Compact",
                _ => "Default"
            }, true);
    }

    private void NavigationViewControl_SelectionChanged(NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavItem item)
            selectHandler?.Invoke(item.View, null, args.RecommendedNavigationTransitionInfo);
    }

    private void NavigationViewControl_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        Root.GoBack();
    }

    private void Hyperlink_OnClick(Hyperlink sender, HyperlinkClickEventArgs args)
    {
        FlyoutBase.ShowAttachedFlyout(CountPanel);
    }
}