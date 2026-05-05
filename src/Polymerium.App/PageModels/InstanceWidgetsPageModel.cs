using Huskui.Avalonia.Mvvm.Activation;
using TridentCore.Core.Services;

namespace Polymerium.App.PageModels;

public class InstanceWidgetsPageModel(
    IViewContext<InstancePageModelBase.InstanceContextParameter> context,
    InstanceManager instanceManager,
    ProfileManager profileManager
) : InstancePageModelBase(context, instanceManager, profileManager)
{ }
