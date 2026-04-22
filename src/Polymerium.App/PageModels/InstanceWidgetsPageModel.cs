using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.Models;
using Polymerium.App.Facilities;
using Trident.Core.Services;

namespace Polymerium.App.PageModels;

public class InstanceWidgetsPageModel(
    IViewContext<InstancePageModelBase.InstanceContextParameter> context,
    InstanceManager instanceManager,
    ProfileManager profileManager
) : InstancePageModelBase(context, instanceManager, profileManager) { }
