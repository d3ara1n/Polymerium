using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.App.ViewModels;

public class AddAccountWizardViewModel : ObservableObject
{
    internal readonly CancellationTokenSource Source = new();
}