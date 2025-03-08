using Polymerium.App.Facilities;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public class InstanceStatisticsViewModel : InstanceViewModelBase
{
    public InstanceStatisticsViewModel(ViewBag bag, InstanceManager instanceManager, ProfileManager profileManager) :
        base(bag, instanceManager, profileManager) { }
}