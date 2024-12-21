using System;
using Avalonia.Animation;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Transitions;
using Polymerium.App.Controls;
using Polymerium.App.Exceptions;
using Polymerium.App.Views;

namespace Polymerium.App.Services;

public class NavigationService(IServiceProvider provider)
{
    private Action<Type, object?, IPageTransition>? _handler;

    public void SetHandler(Action<Type, object?, IPageTransition> handler)
    {
        _handler = handler;
    }

    public void Navigate(Type page, object? parameter = null, IPageTransition? transition = null)
    {
        try
        {
            _handler?.Invoke(page, parameter, transition ?? (page.IsAssignableTo(typeof(ScopedPage))
                ? new PageCoverOverTransition(TimeSpan.FromMilliseconds(197), DirectionFrom.Right)
                : new PopUpTransition(TimeSpan.FromMilliseconds(197))));
        }
        catch (NavigationFailedException ex)
        {
            _handler?.Invoke(typeof(PageNotReachedView), ex.Message,
                new PageCoverOverTransition(null, DirectionFrom.Right));
        }
    }

    public void Navigate<T>(object? parameter = null, IPageTransition? transition = null)
        where T : Page
    {
        Navigate(typeof(T), parameter, transition);
    }
}