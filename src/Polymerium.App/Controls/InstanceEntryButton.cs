using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;
using Polymerium.App.Models;
using System;

namespace Polymerium.App.Controls;

public class InstanceEntryButton : Button
{
    public static readonly DirectProperty<InstanceEntryButton, InstanceBasicModel?> BasicProperty =
        AvaloniaProperty.RegisterDirect<InstanceEntryButton, InstanceBasicModel?>(nameof(Basic), o => o.Basic,
            (o, v) => o.Basic = v);

    public static readonly DirectProperty<InstanceEntryButton, InstanceEntryState> StateProperty =
        AvaloniaProperty.RegisterDirect<InstanceEntryButton, InstanceEntryState>(nameof(State), o => o.State,
            (o, v) => o.State = v);

    public static readonly DirectProperty<InstanceEntryButton, double?> ProgressProperty =
        AvaloniaProperty.RegisterDirect<InstanceEntryButton, double?>(nameof(Progress), o => o.Progress,
            (o, v) => o.Progress = v);

    private InstanceBasicModel? _basic;

    private double? _progress;

    private InstanceEntryState _state = InstanceEntryState.Idle;
    protected override Type StyleKeyOverride => typeof(InstanceEntryButton);

    [Content]
    public InstanceBasicModel? Basic
    {
        get => _basic;
        set => SetAndRaise(BasicProperty, ref _basic, value);
    }

    public InstanceEntryState State
    {
        get => _state;
        set => SetAndRaise(StateProperty, ref _state, value);
    }

    public double? Progress
    {
        get => _progress;
        set => SetAndRaise(ProgressProperty, ref _progress, value);
    }
}