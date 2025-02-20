﻿using Avalonia;
using Avalonia.Controls.Primitives;
using Polymerium.App.Models;
using System.Windows.Input;

namespace Polymerium.App.Controls;

public class LaunchBar : TemplatedControl
{
    public static readonly DirectProperty<LaunchBar, LaunchBarState> StateProperty =
        AvaloniaProperty.RegisterDirect<LaunchBar, LaunchBarState>(nameof(State), o => o.State, (o, v) => o.State = v);

    public static readonly DirectProperty<LaunchBar, ICommand> PlayCommandProperty =
        AvaloniaProperty.RegisterDirect<LaunchBar, ICommand>(nameof(PlayCommand), o => o.PlayCommand,
            (o, v) => o.PlayCommand = v);

    public static readonly DirectProperty<LaunchBar, ICommand> AbortCommandProperty =
        AvaloniaProperty.RegisterDirect<LaunchBar, ICommand>(nameof(AbortCommand), o => o.AbortCommand,
            (o, v) => o.AbortCommand = v);

    public static readonly DirectProperty<LaunchBar, ICommand> DashboardCommandProperty =
        AvaloniaProperty.RegisterDirect<LaunchBar, ICommand>(nameof(DashboardCommand), o => o.DashboardCommand,
            (o, v) => o.DashboardCommand = v);

    private ICommand _abortCommand;

    private ICommand _dashboardCommand;

    private ICommand _playCommand;

    private LaunchBarState _state;

    public LaunchBarState State
    {
        get => _state;
        set => SetAndRaise(StateProperty, ref _state, value);
    }

    public ICommand PlayCommand
    {
        get => _playCommand;
        set => SetAndRaise(PlayCommandProperty, ref _playCommand, value);
    }

    public ICommand AbortCommand
    {
        get => _abortCommand;
        set => SetAndRaise(AbortCommandProperty, ref _abortCommand, value);
    }

    public ICommand DashboardCommand
    {
        get => _dashboardCommand;
        set => SetAndRaise(DashboardCommandProperty, ref _dashboardCommand, value);
    }
}