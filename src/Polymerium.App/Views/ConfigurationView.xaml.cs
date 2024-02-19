// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConfigurationView
    {
        public ConfigurationView()
        {
            InitializeComponent();
        }

        public ConfigurationViewModel ViewModel { get; } = App.ViewModel<ConfigurationViewModel>();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.OnAttached(e.Parameter);
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ViewModel.OnDetached();
            base.OnNavigatedFrom(e);
        }
    }
}