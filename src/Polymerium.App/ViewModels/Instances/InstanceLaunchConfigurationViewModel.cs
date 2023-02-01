using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceLaunchConfigurationViewModel : ObservableObject
{
    public ViewModelContext Context { get; }

    public InstanceLaunchConfigurationViewModel(ViewModelContext context)
    {
        Context = context;
    }
}