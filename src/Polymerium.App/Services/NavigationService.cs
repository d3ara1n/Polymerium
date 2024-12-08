using System;
using Avalonia.Animation;
using Huskui.Avalonia.Controls;

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
        _handler?.Invoke(page, parameter, transition ?? new CrossFade(TimeSpan.FromMilliseconds(150)));
    }

    public void Navigate<T>(object? parameter = null, IPageTransition? transition = null)
        where T : Page
    {
        Navigate(typeof(T), parameter, transition);
    }
}