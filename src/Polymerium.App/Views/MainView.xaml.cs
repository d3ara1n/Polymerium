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
                .Append(new NavigationSearchBarItemModel($"搜索: {input}", "\xE721"));
            sender.ItemsSource = result;
        }
    }

    private void SearchBar_OnSuggestionChosen(
        AutoSuggestBox sender,
        AutoSuggestBoxSuggestionChosenEventArgs args
    )
    {
        sender.Text = string.Empty;
        var model = (NavigationSearchBarItemModel)args.SelectedItem;
        if (string.IsNullOrEmpty(model.Reference))
        {
            // TODO: Go to Search Center
        }
        else
        {
            ViewModel.GotoInstanceView(model.Reference);
        }
    }

    private void SearchBar_OnQuerySubmitted(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args
    )
    {
        // TODO: Go to Search Center
    }
}