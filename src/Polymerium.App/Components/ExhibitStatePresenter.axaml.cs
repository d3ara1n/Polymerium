using Avalonia;
using Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Components;

public partial class ExhibitStatePresenter : UserControl
{
    public static readonly DirectProperty<ExhibitStatePresenter, ExhibitState?> StateProperty =
        AvaloniaProperty.RegisterDirect<ExhibitStatePresenter, ExhibitState?>(nameof(State),
            o => o.State,
            (o, v) => o.State = v);

    public ExhibitStatePresenter() => InitializeComponent();

    public ExhibitState? State
    {
        get;
        set => SetAndRaise(StateProperty, ref field, value);
    }
}