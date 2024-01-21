using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MarketView : Page
{
    public static readonly DependencyProperty HeaderImageProperty = DependencyProperty.Register(nameof(HeaderImage),
        typeof(Brush), typeof(MarketView), new PropertyMetadata(null));

    public MarketView()
    {
        InitializeComponent();
    }

    public MarketViewModel ViewModel { get; } = App.ViewModel<MarketViewModel>();

    public Brush? HeaderImage
    {
        get => (Brush?)GetValue(HeaderImageProperty);
        set => SetValue(HeaderImageProperty, value);
    }

    private void RepositorySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var first = (RepositoryModel?)e.AddedItems.FirstOrDefault();
        if (first != null)
        {
            HeaderImage = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri($"ms-appx://{first.Background}"))
            };
            Submit(first, SearchBox.Text);
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        Submit((RepositoryModel)RepositorySelector.SelectedItem, args.QueryText);
    }

    private void Submit(RepositoryModel repository, string query)
    {
        ViewModel.UpdateSource(repository.Label, query);
    }
}