using Polymerium.App.Facilities;
using Trident.Core.Services;

namespace Polymerium.App.PageModels;

public class InstanceWidgetsPageModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager
) : InstancePageModelBase(bag, instanceManager, profileManager) { }
