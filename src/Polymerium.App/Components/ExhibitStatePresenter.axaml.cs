using Avalonia;
using Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Components;

public class ExhibitStatePresenter : UserControl
{
    public ExhibitStatePresenter()
    {
        InitializeComponent();
    }

    public static readonly DirectProperty<ExhibitStatePresenter, ExhibitState?> StateProperty =
        AvaloniaProperty.RegisterDirect<ExhibitStatePresenter, ExhibitState?>(nameof(State),
                                                                              o => o.State,
                                                                              (o, v) => o.State = v);

    public ExhibitState? State
    {
        get;
        set => SetAndRaise(StateProperty, ref field, value);
    }
}