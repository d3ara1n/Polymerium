using System;
using Avalonia.Animation;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Transitions;
using Microsoft.Extensions.Logging;
using Polymerium.App.Controls;

namespace Polymerium.App.Services;

public class NavigationService
{
    #region Injected

    private readonly ILogger _logger;

    #endregion

    private Func<bool>? _canGoBackHandler;
    private Action? _clearHistoryHandler;
    private Action? _goBackHandler;
    private Action<Type, object?, IPageTransition>? _navigateHandler;

    public NavigationService(ILogger<NavigationService> logger) => _logger = logger;

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
        _navigateHandler?.Invoke(page,
            parameter,
            transition
            ?? (page.IsAssignableTo(typeof(ScopedPage))
                ? new PageCoverOverTransition(null, DirectionFrom.Right)
                : new PopUpTransition()));

    public void Navigate<T>(object? parameter = null, IPageTransition? transition = null) where T : Page
    {
        Navigate(typeof(T), parameter, transition);
        _logger.LogInformation("Navigating to {} with {}", typeof(T).Name, parameter ?? "(null)");
    }

    public void GoBack() => _goBackHandler?.Invoke();

    public void ClearHistory() => _clearHistoryHandler?.Invoke();
}