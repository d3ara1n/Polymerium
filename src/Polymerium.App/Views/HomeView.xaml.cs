using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class HomeView : Page
{
    public HomeView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<HomeViewModel>();
        InitializeComponent();
    }

    public HomeViewModel ViewModel { get; }
}
