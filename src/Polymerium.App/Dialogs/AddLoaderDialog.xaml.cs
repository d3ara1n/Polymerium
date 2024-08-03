// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml;
using Polymerium.App.Models;
using System.Collections.Generic;

namespace Polymerium.App.Dialogs
{
    public sealed partial class AddLoaderDialog
    {
        public AddLoaderDialog(XamlRoot root, string identity, IEnumerable<LoaderVersionModel> versions)
        {
            XamlRoot = root;
            InitializeComponent();
        }
    }
}