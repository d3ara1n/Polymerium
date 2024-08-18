using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.ViewModels;
using Polymerium.Trident.Helpers;
using System;
using System.Linq;
using Windows.Security.Authentication.Web.Core;
using Windows.UI.ApplicationSettings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AccountView : Page
{
    public AccountView() => InitializeComponent();

    public AccountViewModel ViewModel { get; } = App.ViewModel<AccountViewModel>();

    private void Page_Loaded(object sender, RoutedEventArgs e) => ViewModel.OnAttached(null);

    private void Page_Unloaded(object sender, RoutedEventArgs e) => ViewModel.OnDetached();
}