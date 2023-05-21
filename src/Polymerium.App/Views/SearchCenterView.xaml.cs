using System.Linq;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class SearchCenterView : Page
{
    // Using a DependencyProperty as the backing store for IsDataLoading.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsDataLoadingProperty = DependencyProperty.Register(
        nameof(IsDataLoading),
        typeof(bool),
        typeof(SearchCenterView),
        new PropertyMetadata(false)
    );

    public SearchCenterView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<SearchCenterViewModel>();
        InitializeComponent();
    }

    public SearchCenterViewModel ViewModel { get; }

    public bool IsDataLoading
    {
        get => (bool)GetValue(IsDataLoadingProperty);
        set => SetValue(IsDataLoadingProperty, value);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter != null)
        {
            var arguments = (SearchCenterNavigationArguments)e.Parameter;
            SearchBox.Text = arguments.Query;
            var repository = ViewModel.Repositories.FirstOrDefault(
                x => arguments.Repository == null || x.Label == arguments.Repository
            );
            if (repository != null)
            {
                if (!arguments.InstanceScopeOverride)
                    ViewModel.InstanceScope = ViewModel.Instance;
                ViewModel.SelectedRepository = repository;
                var found = true;
                if (arguments.Type != null)
                {
                    var type = ViewModel.SelectedRepository.SupportedResources & arguments.Type;
                    if (type == arguments.Type)
                        ViewModel.SelectedResourceType =
                            ViewModel.SupportedResources.FirstOrDefault(x => x.Tag == arguments.Type);
                    else
                        found = false;
                }

                if (found && arguments.SearchImmediately)
                    QuerySubmitted(arguments.Query);
            }
        }
        else
        {
            ViewModel.InstanceScope = ViewModel.Instance;
            ViewModel.SelectedRepository = ViewModel.Repositories.First();
        }
    }

    private void SearchBox_OnQuerySubmitted(
        AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args
    )
    {
        QuerySubmitted(args.QueryText);
    }

    private void QuerySubmitted(string query)
    {
        if (ViewModel.SelectedResourceType != null)
        {
            var type = ViewModel.SelectedResourceType;
            ResultList.ItemsSource = new IncrementalLoadingCollection<
                IncrementalFactorySource<SearchCenterResultItemModel>,
                SearchCenterResultItemModel
            >(
                new IncrementalFactorySource<SearchCenterResultItemModel>(
                    async (offset, limit, token) =>
                    {
                        IsDataLoading = true;
                        var results = await ViewModel.QueryAsync(query, type.Tag, offset, limit, token);
                        IsDataLoading = false;
                        return results;
                    }
                ),
                10,
                onEndLoading: () =>
                    DispatcherQueue.TryEnqueue(
                        () =>
                            NoResultLabel.Visibility =
                                ResultList.Items.Count > 0
                                    ? Visibility.Collapsed
                                    : Visibility.Visible
                    )
            );
        }
    }

    private void ResultItem_Click(object sender, RoutedEventArgs e)
    {
        var model = (SearchCenterResultItemModel)((Button)sender).DataContext!;
        ViewModel.ShowDetailDialog(model);
    }
}