using System;

namespace Polymerium.App.Services;

public delegate void NavigateHandler(Type view, object? parameter);

public class NavigationService
{
    private NavigateHandler? _handler;

    public void Register(NavigateHandler handler)
    {
        _handler = handler;
    }

    public void Navigate<TView>(object? parameter)
    {
        if (_handler != null)

            _handler(typeof(TView), parameter);
        else
            throw new ArgumentNullException(nameof(_handler));
    }

    public void Navigate<TView>()
    {
        Navigate<TView>(null);
    }
}
