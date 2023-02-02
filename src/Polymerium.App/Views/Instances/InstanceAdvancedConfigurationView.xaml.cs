using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels.Instances;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceAdvancedConfigurationView : Page
{
    public InstanceAdvancedConfigurationView()
    {
        InitializeComponent();
        ViewModel = App.Current.Provider.GetRequiredService<InstanceAdvancedConfigurationViewModel>();
    }

    public InstanceAdvancedConfigurationViewModel ViewModel { get; }
}