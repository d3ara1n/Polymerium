using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Polymerium.App.Services;
using Polymerium.App.Widgets;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public partial class InstanceWidgetsViewModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager,
    WidgetHostService widgetHostService) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    #region Overrides

    protected override Task OnInitializedAsync(CancellationToken token)
    {
        foreach (var type in widgetHostService.WidgetTypes)
        {
            var widget = (WidgetBase)Activator.CreateInstance(type)!;
            widget.Context = widgetHostService.GetOrCreateContext(Basic.Key, type.Name);
            widget.Initialize();
            Widgets.Add(widget);
        }

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync(CancellationToken token)
    {
        foreach (var widget in Widgets)
            widget.Deinitialize();
        Widgets.Clear();
        return Task.CompletedTask;
    }

    #endregion

    #region Reactive

    public ObservableCollection<WidgetBase> Widgets { get; } = [];

    #endregion
}