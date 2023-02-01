using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class HomeView : Page
{
    public HomeView()
    {
        InitializeComponent();
        ViewModel = App.Current.Provider.GetRequiredService<HomeViewModel>();
    }

    public HomeViewModel ViewModel { get; }
}