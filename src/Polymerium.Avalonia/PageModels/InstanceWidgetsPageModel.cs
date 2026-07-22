using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Services;
using TridentCore.Core.Services;

namespace Polymerium.Avalonia.PageModels;

public class InstanceWidgetsPageModel(
    IViewContext<InstancePageModelBase.InstanceContextParameter> context,
    InstanceStateAggregator aggregator,
    InstanceManager instanceManager,
    ProfileManager profileManager) : InstancePageModelBase(context, aggregator, instanceManager, profileManager)
{ }
