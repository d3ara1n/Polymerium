// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Polymerium.App.ViewModels;

namespace Polymerium.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RuntimeView
    {
        public RuntimeViewModel ViewModel { get; } = App.ViewModel<RuntimeViewModel>();

        public RuntimeView()
        {
            this.InitializeComponent();
        }
    }
}