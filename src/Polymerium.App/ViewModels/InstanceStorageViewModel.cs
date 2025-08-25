using Polymerium.App.Facilities;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels
{
    public class InstanceStorageViewModel(ViewBag bag, InstanceManager instanceManager, ProfileManager profileManager)
        : InstanceViewModelBase(bag, instanceManager, profileManager);
}
