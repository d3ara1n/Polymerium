using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels.Instances;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceMetadataConfigurationView : Page
{
    public InstanceMetadataConfigurationView()
    {
        ViewModel =
            App.Current.Provider.GetRequiredService<InstanceMetadataConfigurationViewModel>();
        InitializeComponent();
    }

    public InstanceMetadataConfigurationViewModel ViewModel { get; }
}