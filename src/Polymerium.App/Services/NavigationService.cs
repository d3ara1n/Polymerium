using System;
using Avalonia.Controls;
using FluentAvalonia.UI.Media.Animation;

namespace Polymerium.App.Services;

public class NavigationService
{
    private Action<Type, object?, NavigationTransitionInfo?>? navigationHandler;

    public void RegisterHandler(Action<Type, object?, NavigationTransitionInfo?> handler)
        => navigationHandler = handler;

    public void Navigate<TV>(NavigationTransitionInfo? transition, object? parameter = null)
        where TV : UserControl
        =>Navigate(typeof(TV), transition, parameter);

    public void Navigate<TV>(object? parameter = null)
        where TV : UserControl
        => Navigate<TV>(null, parameter);

    public void Navigate(Type page, NavigationTransitionInfo? transition, object? parameter = null)
        => navigationHandler?.Invoke(page, parameter, transition);
}