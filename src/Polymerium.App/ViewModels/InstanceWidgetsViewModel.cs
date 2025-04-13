using Polymerium.App.Facilities;
using Polymerium.App.Widgets;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public class InstanceWidgetsViewModel : InstanceViewModelBase
{
    public WidgetBase[] Widgets { get; }

    public InstanceWidgetsViewModel(ViewBag bag, InstanceManager instanceManager, ProfileManager profileManager) :
        base(bag, instanceManager, profileManager)
    {
        var context = new WidgetContext();

        Widgets =
        [
            new NoteWidget { Context = context },
            new NoteWidget { Context = context },
            new NoteWidget { Context = context },
            new NoteWidget { Context = context },
            new NoteWidget { Context = context }
        ];
    }
}