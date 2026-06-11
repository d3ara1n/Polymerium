using Avalonia;
using Avalonia.Controls;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Controls;

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
