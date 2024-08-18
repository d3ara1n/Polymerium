using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace Polymerium.App.Services;

public class NavigationService(ILogger<NavigationService> logger)
{
    private Func<bool>? canGoBackHandler;
    private Action? goBackHandler;
    private Action<Type, object?, NavigationTransitionInfo?, bool>? navigateHandler;


    public bool CanGoBack => canGoBackHandler?.Invoke() is true;

    public void SetHandler(Action<Type, object?, NavigationTransitionInfo?, bool> navigate, Action goBack,
        Func<bool> canGoBack)
    {
        navigateHandler = navigate;
        goBackHandler = goBack;
        canGoBackHandler = canGoBack;
    }

    public void Navigate(Type view, object? parameter = null, NavigationTransitionInfo? info = null,
        bool isRoot = false)
    {
        logger.LogInformation("Navigating to {} with \"{}\" as {} in {}", view.Name, parameter,
            isRoot ? "root" : "subpage",
            info?.GetType().Name ?? "default transition");
        navigateHandler?.Invoke(view, parameter, info, isRoot);
    }

    public void GoBack() => goBackHandler?.Invoke();
}