// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs
{
    public sealed partial class ModpackPreviewDialog
    {
        public ModpackPreviewDialog(XamlRoot root, ModpackPreviewModel model)
        {
            XamlRoot = root;
            Model = model;

            InitializeComponent();
        }

        public ModpackPreviewModel Model { get; }
    }
}