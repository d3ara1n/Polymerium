using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class InstanceView : Page
{
    public InstanceView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<InstanceViewModel>();
        InitializeComponent();
    }

    public InstanceViewModel ViewModel { get; }

    private void Header_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        EditButton.Opacity = 1.0;
    }

    private void Header_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        EditButton.Opacity = 0.0;
    }
}
