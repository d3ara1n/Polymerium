using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views.Instances;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceConfigurationViewModel : ObservableObject
{
    private readonly LocalizationService _localizationService;

    public InstanceConfigurationViewModel(
        ViewModelContext context,
        LocalizationService localizationService
    )
    {
        _localizationService = localizationService;
        Instance = context.AssociatedInstance!;
        Pages = new ObservableCollection<InstanceConfigurationPageModel>
        {
            new(
                "ms-appx:///Assets/Icons/icons8-blueprint-48.png",
                _localizationService.GetString("InstanceConfigurationView_Metadata_Label"),
                typeof(InstanceMetadataConfigurationView)
            ),
            new(
                "ms-appx:///Assets/Icons/icons8-firework-48.png",
                _localizationService.GetString("InstanceConfigurationView_Launch_Label"),
                typeof(InstanceLaunchConfigurationView)
            ),
            new(
                "ms-appx:///Assets/Icons/icons8-slider-48.png",
                _localizationService.GetString("InstanceConfigurationView_Advanced_Label"),
                typeof(InstanceAdvancedConfigurationView)
            )
        };
    }

    public GameInstanceModel Instance { get; }

    public ObservableCollection<InstanceConfigurationPageModel> Pages { get; set; }
}
