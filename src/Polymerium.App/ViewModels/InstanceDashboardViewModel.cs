using Polymerium.App.Facilities;
using Trident.Core.Services;

namespace Polymerium.App.ViewModels;

public partial class InstanceDashboardViewModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager) : InstanceViewModelBase(bag, instanceManager, profileManager) { }
