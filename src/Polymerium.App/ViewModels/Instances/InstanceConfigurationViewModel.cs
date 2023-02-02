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
        Context = context;
        Pages = new ObservableCollection<InstanceConfigurationPageModel>
        {
            new InstanceConfigurationPageModel
            {
                Header = "元数据",
                IconSource = "ms-appx:///Assets/Icons/icons8-blueprint-48.png",
                Page = typeof(InstanceMetadataConfigurationView)
            },
            new InstanceConfigurationPageModel
            {
                Header = "启动参数",
                IconSource = "ms-appx:///Assets/Icons/icons8-firework-48.png",
                Page = typeof(InstanceLaunchConfigurationView)
            },
            new InstanceConfigurationPageModel
            {
                Header = "高级",
                IconSource = "ms-appx:///Assets/Icons/icons8-slider-48.png",
                Page = typeof(InstanceAdvancedConfigurationView)
            }
        };
    }

    public ViewModelContext Context { get; }

    public ObservableCollection<InstanceConfigurationPageModel> Pages { get; set; }
}