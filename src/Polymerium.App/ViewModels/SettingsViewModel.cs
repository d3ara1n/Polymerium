using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public partial class SettingsViewModel(ConfigurationService configurationService) : ViewModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial bool IsSuperPowerActivated { get; set; } = configurationService.Value.IsSuperPowerActivated;

    partial void OnIsSuperPowerActivatedChanged(bool value)
    {
        configurationService.Value.IsSuperPowerActivated = value;
    }

    #endregion
}