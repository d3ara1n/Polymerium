using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;
using Polymerium.App.Models;

namespace Polymerium.App.Controls;

public class InstanceEntryButton : Button
{
    protected override Type StyleKeyOverride => typeof(InstanceEntryButton);

    public static readonly DirectProperty<InstanceEntryButton, InstanceBasicModel?> BasicProperty =
        AvaloniaProperty.RegisterDirect<InstanceEntryButton, InstanceBasicModel?>(nameof(Basic), o => o.Basic,
            (o, v) => o.Basic = v);

    private InstanceBasicModel? _basic;

    [Content]
    public InstanceBasicModel? Basic
    {
        get => _basic;
        set => SetAndRaise(BasicProperty, ref _basic, value);
    }

    public static readonly DirectProperty<InstanceEntryButton, InstanceEntryState> StateProperty =
        AvaloniaProperty.RegisterDirect<InstanceEntryButton, InstanceEntryState>(nameof(State), o => o.State,
            (o, v) => o.State = v);

    private InstanceEntryState _state = InstanceEntryState.Idle;

    public InstanceEntryState State
    {
        get => _state;
        set => SetAndRaise(StateProperty, ref _state, value);
    }
}