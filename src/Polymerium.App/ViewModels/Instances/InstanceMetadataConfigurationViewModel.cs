using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceMetadataConfigurationViewModel : ObservableObject
{
    public ViewModelContext Context { get; }

    public InstanceMetadataConfigurationViewModel(ViewModelContext context)
    {
        Context = context;
    }
}