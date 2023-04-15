using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class HomeView : Page
{
    public bool IsNewsLoading
    {
        get => (bool)GetValue(IsNewsLoadingProperty);
        set => SetValue(IsNewsLoadingProperty, value);
    }

    // Using a DependencyProperty as the backing store for IsNewsLoading.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsNewsLoadingProperty =
        DependencyProperty.Register(nameof(IsNewsLoading), typeof(bool), typeof(HomeView), new PropertyMetadata(false));



    public HomeView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<HomeViewModel>();
        InitializeComponent();
    }

    public HomeViewModel ViewModel { get; }

    public ObservableCollection<HomeNewsItemModel> News { get; } = new();

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        IsNewsLoading = true;
        Task.Run(() => ViewModel.LoadNewsAsync(AddNewsHandler));
    }

    private void AddNewsHandler(HomeNewsItemModel? model)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (model != null)
                News.Add(model);
            else
                IsNewsLoading = false;
        });
    }
}