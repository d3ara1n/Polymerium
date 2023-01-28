using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class InstanceView : Page
{
    public InstanceViewModel ViewModel { get; private set; }
    public InstanceView()
    {
        InitializeComponent();

        ViewModel = App.Current.Provider.GetRequiredService<InstanceViewModel>();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var instance = (GameInstance)e.Parameter;
        ViewModel.GotInstance(instance);

    }

    private void Header_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        EditButton.Opacity = 1.0;
    }

    private void Header_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        EditButton.Opacity = 0.0;
    }
}
