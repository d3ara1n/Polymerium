using System;
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
        ViewModel = App.Current.Provider.GetRequiredService<MainViewModel>();
        InitializeComponent();
        ViewModel.SetNavigateHandler(Navigate);
    }

    public MainViewModel ViewModel { get; }

    private void OnSelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args
    )
    {
        var item = sender.SelectedItem as NavigationItemModel;
        if (item != null)
        {
            ViewModel.OnNavigatingTo(item);
            RootFrame.Navigate(
                item.SourcePage,
                item.GameInstance,
                new SuppressNavigationTransitionInfo()
            );
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

    private void Navigate(Type view, object? parameter)
    {
        if (view == typeof(InstanceView) && parameter is string instanceId)
        {
            if (
                ViewModel.NavigationPages
                    .Where(x => x.SourcePage == typeof(InstanceView))
                    .FirstOrDefault(x => x.GameInstance?.Id == instanceId) is
                { } instanceView
            )
                MainNavigationBar.SelectedItem = instanceView;
            else
                throw new ArgumentException($"parameter {parameter} not found as game instance id");
        }
        else
        {
            if (
                ViewModel.NavigationPages.FirstOrDefault(x => x.SourcePage == view) is { } notPinned
            )
                MainNavigationBar.SelectedItem = notPinned;
            else if (
                ViewModel.NavigationPinnedPages.FirstOrDefault(x => x.SourcePage == view) is
                { } pinned
            )
                MainNavigationBar.SelectedItem = pinned;
            else
                RootFrame.Navigate(view, parameter, new SuppressNavigationTransitionInfo());
        }
    }

    private void SearchBar_OnTextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args
    )
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var input = sender.Text;

            var result = ViewModel
                .GetViewOfInstance()
                .Where(x => string.IsNullOrEmpty(input) || x.Name.StartsWith(sender.Text))
                .Select(x => new NavigationSearchBarItemModel(x.Name, "\xF158", x.Id))
                .Append(new NavigationSearchBarItemModel($"搜索: {input}", "\xE721", query: input));
            sender.ItemsSource = result;
        }
    }

    private void SearchBar_OnQuerySubmitted(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args
    )
    {
        if (args.ChosenSuggestion is NavigationSearchBarItemModel model)
        {
            if (string.IsNullOrEmpty(model.Reference))
                ViewModel.GotoSearchView(model.Query!);
            else
                ViewModel.GotoInstanceView(model.Reference);
        }
        else if (!string.IsNullOrEmpty(sender.Text))
        {
            ViewModel.GotoSearchView(sender.Text);
        }
    }

    private void NotificationItem_Loaded(object sender, RoutedEventArgs e)
    {
        var grid = (Grid)sender;
        var fadeIn = new Storyboard();
        Storyboard.SetTarget(fadeIn, grid);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");
        fadeIn.Children.Add(new DoubleAnimation
        {
            From = 0d,
            To = 1d,
            Duration = new Duration(TimeSpan.FromMilliseconds(220))
        });
        fadeIn.Completed += (_, _) =>
        {
            var fadeOut = new Storyboard();
            Storyboard.SetTarget(fadeOut, grid);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");
            fadeOut.Children.Add(new DoubleAnimation
            {
                From = 1d,
                To = 0d,
                BeginTime = TimeSpan.FromSeconds(4),
                Duration = new Duration(TimeSpan.FromMilliseconds(300))
            });
            fadeOut.Completed += (_, _) =>
            {
                if (ViewModel.Notifications.Count > 0)
                    ViewModel.Notifications.RemoveAt(0);
            };
            fadeOut.Begin();
        };
        fadeIn.Begin();
    }
}