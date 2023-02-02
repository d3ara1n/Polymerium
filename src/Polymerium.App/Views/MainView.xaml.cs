using System;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class MainView : Page
{
    public MainView()
    {
        InitializeComponent();
        ViewModel = App.Current.Provider.GetRequiredService<MainViewModel>();
        ViewModel.SetNavigateHandler(Navigate);

        ViewModel.LogonAccounts.CollectionChanged += LogonAccounts_CollectionChanged;
    }

    public MainViewModel ViewModel { get; }

    private void LogonAccounts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:

                foreach (AccountItemModel a in e.NewItems)
                {
                    var item = new MenuFlyoutItem { Text = a.Inner.Nickname, Tag = a };
                    item.Click += (sender, _) =>
                        ViewModel.SwitchAccountTo((AccountItemModel)((MenuFlyoutItem)sender).Tag);
                    SwitchToSubMenu.Items.Add(item);
                }

                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (AccountItemModel a in e.OldItems)
                {
                    var item = SwitchToSubMenu.Items.FirstOrDefault(x =>
                        ((AccountItemModel)x.Tag).Inner.Id == a.Inner.Id);
                    if (item != null)
                        SwitchToSubMenu.Items.Remove(item);
                }

                break;

            case NotifyCollectionChangedAction.Reset:
                SwitchToSubMenu.Items.Clear();
                break;
        }
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var item = sender.SelectedItem as NavigationItemModel;
        if (item != null)
        {
            ViewModel.OnNavigatingTo(item);
            RootFrame.Navigate(item.SourcePage, item.GameInstance, new SuppressNavigationTransitionInfo());
            RootFrame.BackStack.Clear();
        }
        else
        {
            ViewModel.SelectedPage = ViewModel.NavigationPages[0];
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        RootFrame.GoBack();
    }

    private void Navigate(Type view, object parameter)
    {
        if (view == typeof(InstanceView) && parameter is string instanceId)
        {
            if (ViewModel.NavigationPages.Where(x => x.SourcePage == typeof(InstanceView))
                    .FirstOrDefault(x => x.GameInstance.Id == instanceId) is { } instanceView)
                MainNavigationBar.SelectedItem = instanceView;
            else
                throw new ArgumentException($"parameter {parameter} not found as game instance id");
        }
        else
        {
            if (ViewModel.NavigationPages.FirstOrDefault(x => x.SourcePage == view) is
                {
                } notPinned)
                MainNavigationBar.SelectedItem = notPinned;
            else if (ViewModel.NavigationPinnedPages.FirstOrDefault(x => x.SourcePage == view) is
                     {
                     } pinned)
                MainNavigationBar.SelectedItem = pinned;
            else
                RootFrame.Navigate(view, parameter, new SuppressNavigationTransitionInfo());
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.FillMenuItems();
    }
}