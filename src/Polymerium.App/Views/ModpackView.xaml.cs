using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views
{
    public sealed partial class ModpackView : Page
    {
        public ModpackViewModel ViewModel { get; } = App.ViewModel<ModpackViewModel>();

        public ModpackView()
        {
            this.InitializeComponent();
        }
    }
}
