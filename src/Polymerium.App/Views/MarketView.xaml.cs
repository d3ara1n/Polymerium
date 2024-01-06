using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;
using System.Linq;
using System.Threading;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MarketView : Page
    {
        public MarketViewModel ViewModel { get; } = App.ViewModel<MarketViewModel>();

        public MarketView()
        {
            this.InitializeComponent();
        }

        private void RepositorySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var first = (RepositoryModel?)e.AddedItems.FirstOrDefault();
            if (first != null)
            {
                HeaderPane.Background = first.Background;

                Submit(first, SearchBox.Text);
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            Submit((RepositoryModel)RepositorySelector.SelectedItem, args.QueryText);
        }

        private void Submit(RepositoryModel repository, string query)
        {
            ViewModel.UpdateSource(repository.Inner, query, CancellationToken.None);
        }
    }
}
