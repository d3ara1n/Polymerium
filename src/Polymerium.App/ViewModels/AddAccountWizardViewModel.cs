using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading;

namespace Polymerium.App.ViewModels;

public class AddAccountWizardViewModel : ObservableObject
{
    internal readonly CancellationTokenSource Source = new();
}
