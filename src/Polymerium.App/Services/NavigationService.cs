using System;
using Microsoft.UI.Xaml.Media.Animation;

namespace Polymerium.App.Services;

public delegate void NavigateHandler(
    Type view,
    object? parameter,
    NavigationTransitionInfo? transitionInfo
);

public class NavigationService
{
    private NavigateHandler? _handler;

    public void Register(NavigateHandler handler)
    {
        _handler = handler;
    }

    public void Navigate<TView>(object? parameter, NavigationTransitionInfo? transitionInfo)
    {
        if (_handler != null)
            _handler(typeof(TView), parameter, transitionInfo);
        else
            throw new ArgumentNullException(nameof(_handler));
    }

    public void Navigate<TView>(object? parameter)
    {
        Navigate<TView>(parameter, null);
    }

    public void Navigate<TView>()
    {
        Navigate<TView>(null, null);
    }

    public void Navigate<TView>(NavigationTransitionInfo transitionInfo)
    {
        Navigate<TView>(null, transitionInfo);
    }
}
