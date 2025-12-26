using Avalonia.Animation;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Transitions;

namespace Polymerium.App.Modals;

public partial class TrophyModal : Modal, IPageTransitionOverride
{
    public TrophyModal()
    {
        InitializeComponent();
    }

    public IPageTransition TransitionOverride { get; } = new TrophyTransition();
}
