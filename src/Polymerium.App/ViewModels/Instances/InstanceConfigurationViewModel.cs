using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views.Instances;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceConfigurationViewModel : ObservableObject
{
    public InstanceConfigurationViewModel(ViewModelContext context)
    {
        Instance = context.AssociatedInstance!;
        Pages = new ObservableCollection<InstanceConfigurationPageModel>
        {
            new(
                "ms-appx:///Assets/Icons/icons8-blueprint-48.png",
                "元数据",
                typeof(InstanceMetadataConfigurationView)
            ),
            new(
                "ms-appx:///Assets/Icons/icons8-firework-48.png",
                "启动参数",
                typeof(InstanceLaunchConfigurationView)
            ),
            new(
                "ms-appx:///Assets/Icons/icons8-slider-48.png",
                "高级",
                typeof(InstanceAdvancedConfigurationView)
            )
        };
    }

    public GameInstanceModel Instance { get; }

    public ObservableCollection<InstanceConfigurationPageModel> Pages { get; set; }
}
