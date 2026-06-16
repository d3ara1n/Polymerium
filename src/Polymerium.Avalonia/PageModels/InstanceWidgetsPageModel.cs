using Huskui.Avalonia.Mvvm.Activation;
using TridentCore.Core.Services;
using Polymerium.Avalonia.Services;

namespace Polymerium.Avalonia.PageModels;

public class InstanceWidgetsPageModel(
    IViewContext<InstancePageModelBase.InstanceContextParameter> context,
    InstanceStateAggregator aggregator,
    InstanceManager instanceManager,
    ProfileManager profileManager
) : InstancePageModelBase(context, aggregator, instanceManager, profileManager)
{ }
