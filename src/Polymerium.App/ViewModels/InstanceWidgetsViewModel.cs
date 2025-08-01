using Polymerium.App.Facilities;
using Polymerium.App.Widgets;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public class InstanceWidgetsViewModel : InstanceViewModelBase
{
    public InstanceWidgetsViewModel(ViewBag bag, InstanceManager instanceManager, ProfileManager profileManager) :
        base(bag, instanceManager, profileManager)
    {
        var context = new WidgetContext();

        Widgets =
        [
            new NoteWidget { Context = context },
            new NetworkCheckerWidget { Context = context },
            new DummyWidget { Context = context, Title = "Log Viewer" },
            new DummyWidget { Context = context, Title = "Nbt Editor" },
            new DummyWidget { Context = context, Title = "IDK" }
        ];
    }

    public WidgetBase[] Widgets { get; }
}