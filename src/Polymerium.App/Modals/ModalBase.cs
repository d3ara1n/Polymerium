using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Polymerium.App.Modals;

public class ModalBase : UserControl
{
    public ICommand DismissCommand { get; internal set; } = null!;
}