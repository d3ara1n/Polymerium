using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class InstanceLaunchBarModel : ModelBase
{
    #region Reactive Properties

    [ObservableProperty]
    private LaunchBarState _state = LaunchBarState.Idle;

    #endregion
}