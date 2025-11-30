using Polymerium.App.Facilities;
using Polymerium.App.Services;
using Trident.Core.Services;

namespace Polymerium.App.ViewModels;

public class InstanceDashboardViewModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager,
    PersistenceService persistenceService,
    ScrapService scrapService) : InstanceViewModelBase(bag, instanceManager, profileManager) { }
