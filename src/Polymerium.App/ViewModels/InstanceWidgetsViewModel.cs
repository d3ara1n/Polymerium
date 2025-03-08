using Polymerium.App.Facilities;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public class InstanceWidgetsViewModel : InstanceViewModelBase
{
    public InstanceWidgetsViewModel(ViewBag bag, InstanceManager instanceManager, ProfileManager profileManager) :
        base(bag, instanceManager, profileManager) { }
}