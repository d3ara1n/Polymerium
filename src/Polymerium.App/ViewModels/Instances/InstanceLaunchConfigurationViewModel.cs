using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceLaunchConfigurationViewModel : ObservableObject
{
    public InstanceLaunchConfigurationViewModel(ViewModelContext context)
    {
        Context = context;
    }

    public ViewModelContext Context { get; }
}