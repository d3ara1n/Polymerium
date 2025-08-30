using Avalonia;
using Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Controls;

public class InstanceEntryButton : Button
{
    public static readonly StyledProperty<InstanceEntryState> StateProperty =
        AvaloniaProperty.Register<InstanceEntryButton, InstanceEntryState>(nameof(State));


    public InstanceEntryState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }
}
