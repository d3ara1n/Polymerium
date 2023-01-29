using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading;

namespace Polymerium.App.ViewModels
{
    public partial class AddAccountWizardViewModel : ObservableObject
    {
        internal readonly CancellationTokenSource Source = new();

        public AddAccountWizardViewModel()
        {
        }
    }
}