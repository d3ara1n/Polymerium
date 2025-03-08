using System;
using Avalonia.Animation;
using Avalonia.Threading;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Transitions;
using Polymerium.App.Controls;

namespace Polymerium.App.Services;

public class NavigationService
{
    private Action<Type, object?, IPageTransition>? _handler;

    public void SetHandler(Action<Type, object?, IPageTransition> handler) => _handler = handler;

    public void Navigate(Type page, object? parameter = null, IPageTransition? transition = null) =>
        Dispatcher.UIThread.Post(() => _handler?.Invoke(page,
                                                        parameter,
                                                        transition
                                                     ?? (page.IsAssignableTo(typeof(ScopedPage))
                                                             ? new PageCoverOverTransition(null, DirectionFrom.Right)
                                                             : new PopUpTransition())));

    public void Navigate<T>(object? parameter = null, IPageTransition? transition = null) where T : Page =>
        Navigate(typeof(T), parameter, transition);
}