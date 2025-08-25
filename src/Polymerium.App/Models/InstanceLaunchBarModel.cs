using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models
{
    public partial class InstanceLaunchBarModel : ModelBase
    {
        #region Reactive

        [ObservableProperty]
        public partial LaunchBarState State { get; set; } = LaunchBarState.Idle;

        #endregion
    }
}
