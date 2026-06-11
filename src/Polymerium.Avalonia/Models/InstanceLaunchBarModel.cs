using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public partial class InstanceLaunchBarModel : ModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial LaunchBarState State { get; set; } = LaunchBarState.Idle;

    #endregion
}
