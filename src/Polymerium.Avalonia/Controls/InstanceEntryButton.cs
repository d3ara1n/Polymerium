using Avalonia;
using Avalonia.Controls;
using TridentCore.Abstractions;

namespace Polymerium.Avalonia.Controls;

public class InstanceEntryButton : Button
{
    public static readonly StyledProperty<InstanceState> StateProperty =
        AvaloniaProperty.Register<InstanceEntryButton, InstanceState>(nameof(State));

    public InstanceState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }
}
