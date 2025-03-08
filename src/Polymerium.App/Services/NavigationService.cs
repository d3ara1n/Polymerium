using System;
using Avalonia.Animation;
using Avalonia.Threading;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Transitions;
using Polymerium.App.Controls;

namespace Polymerium.App.Services;

public class NavigationService
{
    private Action<Type, object?, IPageTransition>? _navigateHandler;
    private Action? _goBackHandler;
    private Func<bool>? _canGoBackHandler;

    public bool CanGoBack => _canGoBackHandler?.Invoke() ?? false;

    public void SetHandler(
        Action<Type, object?, IPageTransition> navigateHandler,
        Action goBackHandler,
        Func<bool> canGoBackHandler)
    {
        _navigateHandler = navigateHandler;
        _goBackHandler = goBackHandler;
        _canGoBackHandler = canGoBackHandler;
    }

    public void Navigate(Type page, object? parameter = null, IPageTransition? transition = null) =>
        Dispatcher.UIThread.Post(() => _navigateHandler?.Invoke(page,
                                                                parameter,
                                                                transition
                                                             ?? (page.IsAssignableTo(typeof(ScopedPage))
                                                                     ? new PageCoverOverTransition(null,
                                                                         DirectionFrom.Right)
                                                                     : new PopUpTransition())));

    public void Navigate<T>(object? parameter = null, IPageTransition? transition = null) where T : Page =>
        Navigate(typeof(T), parameter, transition);

    public void GoBack() => Dispatcher.UIThread.Post(() => _goBackHandler?.Invoke());
}