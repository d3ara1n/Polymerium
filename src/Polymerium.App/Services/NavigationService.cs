using System;
using Avalonia.Animation;
using Avalonia.Threading;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Transitions;
using Polymerium.App.Controls;

namespace Polymerium.App.Services;

public class NavigationService
{
    private Func<bool>? _canGoBackHandler;
    private Action? _clearHistoryHandler;
    private Action? _goBackHandler;
    private Action<Type, object?, IPageTransition>? _navigateHandler;

    public bool CanGoBack => _canGoBackHandler?.Invoke() ?? false;

    public void SetHandler(
        Action<Type, object?, IPageTransition> navigateHandler,
        Action goBackHandler,
        Func<bool> canGoBackHandler,
        Action clearHistoryHandler)
    {
        _navigateHandler = navigateHandler;
        _goBackHandler = goBackHandler;
        _canGoBackHandler = canGoBackHandler;
        _clearHistoryHandler = clearHistoryHandler;
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

    public void ClearHistory() => Dispatcher.UIThread.Post(() => _clearHistoryHandler?.Invoke());
}