using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DashboardView : Page
{
    public DashboardView() => InitializeComponent();

    public DashboardViewModel ViewModel { get; } = App.ViewModel<DashboardViewModel>();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.OnAttached(e.Parameter);
        base.OnNavigatedTo(e);
    }
}