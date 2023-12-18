using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InstanceDetailView : Page
    {
        public InstanceDetailViewModel ViewModel { get; }

        private object? parameter;

        public InstanceDetailView()
        {
            ViewModel = App.ViewModel<InstanceDetailViewModel>();

            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            parameter = e.Parameter;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.OnAttached(parameter);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.OnDetached();
        }
    }
}
