using System;
using Avalonia.Interactivity;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Windows Only
        // TitleBar.ExtendsContentIntoTitleBar = true;
        
        ViewModelLocator.Current.Provider.GetRequiredService<NavigationService>().RegisterHandler(Navigate);
        
        Navigation.MenuItemsSource = ViewModelLocator.Current.MainNavItems;
        Navigation.FooterMenuItemsSource = ViewModelLocator.Current.FooterNavItems;
        var first = Navigation.MenuItemsSource.ElementAt(0) as PageLink;
        Navigation.SelectedItem = first;
        Navigate(first!.Page);
    }

    private void Navigate(Type pageType, object? parameter = null, NavigationTransitionInfo? transition = null)
    {
        if (transition != null)
        {
            Root.Navigate(pageType, parameter);
        }
        else if (parameter != null)
        {
            Root.Navigate(pageType, parameter);
        }
        else
        {
            Root.Navigate(pageType);
        }
    }

    private void Navigation_OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.Tag is PageLink link)
            ViewModelLocator.Current.Provider.GetRequiredService<NavigationService>()
                .Navigate(link.Page, e.RecommendedNavigationTransitionInfo);
    }
}