using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions;
using Polymerium.App.Models;
using Polymerium.App.Services;
using System.Collections.ObjectModel;
using Polymerium.App.Views.Instances;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceConfigurationViewModel : ObservableObject
{
    private readonly ViewModelContext _context;
    public ViewModelContext Context => _context;
    public ObservableCollection<InstanceConfigurationPageModel> Pages { get; set; }

    public InstanceConfigurationViewModel(ViewModelContext context)
    {
        _context = context;
        Pages = new()
        {
            new()
            {
                Header = "元数据",
                IconSource = "ms-appx:///Assets/Icons/icons8-blueprint-48.png",
                Page = typeof(InstanceMetadataConfigurationView)
            },
            new()
            {
                Header = "启动参数",
                IconSource = "ms-appx:///Assets/Icons/icons8-firework-48.png",
                Page = typeof(InstanceLaunchConfigurationView)
            },
            new()
            {
                Header = "高级",
                IconSource = "ms-appx:///Assets/Icons/icons8-slider-48.png",
                Page = typeof(InstanceAdvancedConfigurationView)
            }
        };
    }
}