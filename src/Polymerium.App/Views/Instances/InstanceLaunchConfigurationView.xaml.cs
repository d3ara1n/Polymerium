using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels.Instances;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceLaunchConfigurationView : Page
{
    public InstanceLaunchConfigurationView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<InstanceLaunchConfigurationViewModel>();
        InitializeComponent();
    }

    public InstanceLaunchConfigurationViewModel ViewModel { get; }
}
