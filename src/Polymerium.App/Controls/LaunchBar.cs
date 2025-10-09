using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Trident.Core;

namespace Polymerium.App.Controls;

public class LaunchBar : TemplatedControl
{
    public static readonly DirectProperty<LaunchBar, InstanceState> StateProperty =
        AvaloniaProperty.RegisterDirect<LaunchBar, InstanceState>(nameof(State), o => o.State, (o, v) => o.State = v);

    public static readonly DirectProperty<LaunchBar, ICommand?> PlayCommandProperty =
        AvaloniaProperty.RegisterDirect<LaunchBar, ICommand?>(nameof(PlayCommand),
                                                              o => o.PlayCommand,
                                                              (o, v) => o.PlayCommand = v);

    public static readonly DirectProperty<LaunchBar, ICommand?> AbortCommandProperty =
        AvaloniaProperty.RegisterDirect<LaunchBar, ICommand?>(nameof(AbortCommand),
                                                              o => o.AbortCommand,
                                                              (o, v) => o.AbortCommand = v);

    public static readonly DirectProperty<LaunchBar, ICommand?> DashboardCommandProperty =
        AvaloniaProperty.RegisterDirect<LaunchBar, ICommand?>(nameof(DashboardCommand),
                                                              o => o.DashboardCommand,
                                                              (o, v) => o.DashboardCommand = v);

    public InstanceState State
    {
        get;
        set => SetAndRaise(StateProperty, ref field, value);
    }

    public ICommand? PlayCommand
    {
        get;
        set => SetAndRaise(PlayCommandProperty, ref field, value);
    }

    public ICommand? AbortCommand
    {
        get;
        set => SetAndRaise(AbortCommandProperty, ref field, value);
    }

    public ICommand? DashboardCommand
    {
        get;
        set => SetAndRaise(DashboardCommandProperty, ref field, value);
    }
}
