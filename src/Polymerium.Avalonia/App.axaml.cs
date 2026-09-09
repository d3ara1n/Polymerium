using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Huskui.Avalonia;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Mvvm.Activation;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Services;
using Sentry;
using TridentCore.Core.Lifetimes;

namespace Polymerium.Avalonia;

public class App : Application
{
    private static ExitState _exitState;

    public HuskuiTheme? Theme { get; private set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ErrorReporter.Report(e.ExceptionObject,
            new(ErrorReporter.ErrorReportSource.AppDomainUnhandled,
                "runtime",
                true,
                e.IsTerminating,
                e.IsTerminating ? SentryLevel.Fatal : SentryLevel.Error));
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            // 网络/传输层异常（代理/VPN/防火墙致 TLS 握手损坏等）是用户环境问题而非应用 bug，
            // 吞掉避免崩溃，但仍以 Warning 上报 Sentry，便于区分网络代码错误与用户环境问题。
            if (IsNetworkRelatedException(e.Exception))
            {
                e.SetObserved();
                ErrorReporter.Report(e.Exception,
                                     new(ErrorReporter.ErrorReportSource.NetworkUnobserved,
                                         "runtime",
                                         false,
                                         false,
                                         SentryLevel.Warning));
                return;
            }

            ErrorReporter.Report(e.Exception,
                                 new(ErrorReporter.ErrorReportSource.TaskUnobserved,
                                     "runtime",
                                     true,
                                     false,
                                     SentryLevel.Warning));
        };
        Dispatcher.UIThread.UnhandledException += (_, e) => ErrorReporter.Report(e.Exception,
            new(ErrorReporter.ErrorReportSource.DispatcherUnhandled,
                "runtime",
                true,
                !e.Handled,
                !e.Handled ? SentryLevel.Fatal : SentryLevel.Error));

        foreach (var styles in Styles)
        {
            if (styles is HuskuiTheme husk)
            {
                Theme = husk;
                break;
            }
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // macOS 关闭主窗口不退出应用，允许经 Dock 栏重新打开。
            if (OperatingSystem.IsMacOS())
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }

            desktop.MainWindow = ConstructWindow();
            desktop.ShutdownRequested += (_, e) => OnShutdownRequested(desktop, e);
            _ = StartLifetimeServicesAsync(desktop);
        }

        // macOS Dock 栏点击重新打开主窗口。
        if (Current?.TryGetFeature<IActivatableLifetime>() is { } activatable)
        {
            activatable.Activated += OnActivated;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    ///     判断异常链中是否包含网络/传输层异常。
    ///     用于在 UnobservedTaskException 中区分用户环境导致的网络失败（代理、VPN、防火墙破坏 TLS 握手等），
    ///     这类异常不是应用 bug，吞掉后仍以 Warning 级别上报 Sentry，便于与真正的应用 bug 区分排查。
    /// </summary>
    private static bool IsNetworkRelatedException(Exception? exception)
    {
        HashSet<Exception>? visited = null;
        while (exception is not null)
        {
            visited ??= [];
            if (!visited.Add(exception))
            {
                break;
            }

            // WARNING: HttpRequestException/SocketException/AuthenticationException 均视为传输层问题，
            //  不并入 IOException，避免本地文件异常被误判为网络问题而吞掉。
            if (exception is HttpRequestException or SocketException or AuthenticationException)
            {
                return true;
            }

            if (exception is AggregateException ae)
            {
                foreach (var inner in ae.InnerExceptions)
                {
                    if (IsNetworkRelatedException(inner))
                    {
                        return true;
                    }
                }
            }

            exception = exception.InnerException;
        }

        return false;
    }

    private static async Task StartLifetimeServicesAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (Program.Services?.GetService<LifetimeServiceRuntime>() is not { } runtime)
        {
            return;
        }

        try
        {
            await runtime.StartAsync();
        }
        catch (Exception ex)
        {
            ErrorReporter.Report(ex,
                                 new(ErrorReporter.ErrorReportSource.LifetimeStartup,
                                     "startup",
                                     true,
                                     true,
                                     SentryLevel.Fatal));
            Dispatcher.UIThread.Post(() =>
            {
                _exitState = ExitState.Ready;
                desktop.Shutdown(-1);
            });
        }
    }

    private static void OnActivated(object? sender, ActivatedEventArgs e)
    {
        if (e.Kind != ActivationKind.Reopen)
        {
            return;
        }

        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        EnsureMainWindow(desktop);
    }

    // 主窗口非空引用必然是活窗口（Closed 时已置空，见 ConstructWindow）：
    // 藏起的 Show 回来、最小化的还原；置空才需要重建走完整生命周期。
    private static void EnsureMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (desktop.MainWindow is not { } window)
        {
            window = ConstructWindow();
            desktop.MainWindow = window;
            window.Show();
        }
        else if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }

    #region Exit Confirmation

    private static async void OnShutdownRequested(
        IClassicDesktopStyleApplicationLifetime desktop,
        ShutdownRequestedEventArgs e)
    {
        if (_exitState is ExitState.Ready || Program.Services?.GetService<ExitGuardService>() is not { } guard)
        {
            return;
        }

        e.Cancel = true;
        await RequestExitAsync(desktop, guard, () => desktop.Shutdown());
    }

    internal static async Task RequestExitAsync(
        IClassicDesktopStyleApplicationLifetime desktop,
        ExitGuardService guard,
        Action proceed)
    {
        if (_exitState is not ExitState.Idle)
        {
            return;
        }

        _exitState = ExitState.Confirming;
        try
        {
            if (guard.IsBusy)
            {
                EnsureMainWindow(desktop);
                if (!await guard.RequestConfirmationAsync())
                {
                    return;
                }
            }

            _exitState = ExitState.Stopping;
            await guard.StopAsync();
            // NOTE: An idle stop can complete synchronously; let the original Closing event return before closing again.
            await Dispatcher.Yield();
            _exitState = ExitState.Ready;
            proceed();
        }
        finally
        {
            if (_exitState is ExitState.Confirming)
            {
                _exitState = ExitState.Idle;
            }
        }
    }

    private enum ExitState { Idle, Confirming, Stopping, Ready }

    #endregion

    internal static Window ConstructWindow()
    {
        if (Program.Services is null)
        {
            return new();
        }

        Program.Services.GetService<FontService>()?.ApplyFromConfiguration();

        var configuration = Program.Services.GetRequiredService<ConfigurationService>();
        var window = new MainWindow();

        #region Wire

        var navigation = Program.Services.GetRequiredService<NavigationService>();
        navigation.SetHandler(window.Navigate, window.GoBack, window.CanGoBack, window.ClearHistory);

        var activator = Program.Services.GetRequiredService<IViewActivator>();
        window.SetFrameActivator(activator);

        var overlay = Program.Services.GetRequiredService<OverlayService>();
        overlay.SetHandler(window.PopToast, window.PopSidebar, window.PopModal, window.PopDialog);

        var notification = Program.Services.GetRequiredService<NotificationService>();
        notification.SetHandler(window.PopGrowl);

        // WARNING: 卸载时断开服务 handler，防止悬空引用。
        window.Unloaded += (_, _) =>
        {
            navigation.SetHandler(null!, null!, null!, null!);
            overlay.SetHandler(null!, null!, null!, null!);
            notification.SetHandler((Action<GrowlItem>?)null!);
        };

        // NOTE: Close 后的 Window 不可再 Show（PlatformImpl 已销毁）；置空引用，
        //  让需要主窗口的路径（Dock 重开、退出确认）走重建而不是复活已关闭的窗口。
        window.Closed += (_, _) =>
        {
            if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow == window)
            {
                desktop.MainWindow = null;
            }
        };

        #endregion

        #region Window Size Persistence

        window.Opened += (_, _) =>
        {
            var w = configuration.Value.ApplicationWindowWidth;
            var h = configuration.Value.ApplicationWindowHeight;
            if (w > 0 && h > 0)
            {
                window.Width = w;
                window.Height = h;
            }
        };

        window.Closing += async (_, e) =>
        {
            configuration.Value.ApplicationWindowWidth = window.Width;
            configuration.Value.ApplicationWindowHeight = window.Height;

            if (_exitState is ExitState.Ready)
            {
                return;
            }

            // NOTE: Keep the overlay host alive throughout confirmation and settlement, including on macOS.
            if (_exitState is not ExitState.Idle)
            {
                e.Cancel = true;
                return;
            }

            if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.ShutdownMode is not ShutdownMode.OnExplicitShutdown
                && Program.Services.GetService<ExitGuardService>() is { } guard)
            {
                e.Cancel = true;
                await RequestExitAsync(desktop, guard, window.Close);
            }
        };

        #endregion

        var themeService = Program.Services.GetRequiredService<ThemeService>();
        window.AttachTheme(themeService);

        // WARNING: 须在窗口初始化之后——MainWindowContext 构造函数要求 window 已与服务绑定。
        var viewModel = ActivatorUtilities.CreateInstance<MainWindowContext>(Program.Services);
        window.DataContext = viewModel;

        notification.SetHandler(notification.PopNotification);

        navigation.Navigate<LandingPage>();

        return window;
    }
}
