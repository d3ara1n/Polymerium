using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceConfigurationView : Page
{
    public InstanceConfigurationViewModel ViewModel { get; }

    public InstanceConfigurationView()
    {
        InitializeComponent();
        ViewModel = App.Current.Provider.GetRequiredService<InstanceConfigurationViewModel>();
    }
}