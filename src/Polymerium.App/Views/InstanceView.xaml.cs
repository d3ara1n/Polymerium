using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.Abstractions;
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

        var instance = e.Parameter as GameInstance;
        ViewModel.GotInstance(instance);

    }
}
