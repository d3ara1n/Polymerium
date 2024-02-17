using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;

namespace Polymerium.App.Modals;

public class ModalBase : UserControl
{
    public ICommand DismissCommand { get; internal set; } = null!;
}