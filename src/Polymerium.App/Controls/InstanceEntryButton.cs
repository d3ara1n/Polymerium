using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;
using Polymerium.App.Models;

namespace Polymerium.App.Controls;

public class InstanceEntryButton : Button
{
    public static readonly StyledProperty<InstanceBasicModel?> BasicProperty =
        AvaloniaProperty.Register<InstanceEntryButton, InstanceBasicModel?>(nameof(Basic));

    public static readonly StyledProperty<InstanceEntryState> StateProperty =
        AvaloniaProperty.Register<InstanceEntryButton, InstanceEntryState>(nameof(State));

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<InstanceEntryButton, double>(nameof(Progress));

    public static readonly StyledProperty<bool> IsPendingProperty =
        AvaloniaProperty.Register<InstanceEntryButton, bool>(nameof(IsPending));

    [Content]
    public InstanceBasicModel? Basic
    {
        get => GetValue(BasicProperty);
        set => SetValue(BasicProperty, value);
    }


    public InstanceEntryState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public bool IsPending
    {
        get => GetValue(IsPendingProperty);
        set => SetValue(IsPendingProperty, value);
    }
}