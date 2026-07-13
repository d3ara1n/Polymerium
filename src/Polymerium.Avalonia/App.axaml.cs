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
    public HuskuiTheme? Theme { get; private set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ErrorReporter.Report(
                e.ExceptionObject,
                new(
                    ErrorReporter.ErrorReportSource.AppDomainUnhandled,
                    Phase: "runtime",
                    Critical: true,
                    Terminating: e.IsTerminating,
                    Level: e.IsTerminating ? SentryLevel.Fatal : SentryLevel.Error
                )
            );
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            // 网络/传输层异常（代理、VPN/梯子、防火墙导致的 TLS 握手损坏等）
            // 属于用户环境问题而非应用 bug，吞掉以避免崩溃，但仍以 Warning 级别上报 Sentry，
            // 便于区分真正的网络代码错误与用户侧网络环境问题。
            if (IsNetworkRelatedException(e.Exception))
            {
                e.SetObserved();
                ErrorReporter.Report(
                    e.Exception,
                    new(
                        ErrorReporter.ErrorReportSource.NetworkUnobserved,
                        Phase: "runtime",
                        Critical: false,
                        Terminating: false,
                        Level: SentryLevel.Warning
                    )
                );
                return;
            }

            ErrorReporter.Report(
                e.Exception,
                new(
                    ErrorReporter.ErrorReportSource.TaskUnobserved,
                    Phase: "runtime",
                    Critical: true,
                    Terminating: false,
                    Level: SentryLevel.Warning
                )
            );
        };
        Dispatcher.UIThread.UnhandledException += (_, e) =>
            ErrorReporter.Report(
                e.Exception,
                new(
                    ErrorReporter.ErrorReportSource.DispatcherUnhandled,
                    Phase: "runtime",
                    Critical: true,
                    Terminating: !e.Handled,
                    Level: !e.Handled ? SentryLevel.Fatal : SentryLevel.Error
                )
            );

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
            // macOS: 关闭主窗口不退出应用，允许通过 Dock 栏重新打开
            if (OperatingSystem.IsMacOS())
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }

            desktop.MainWindow = ConstructWindow();
            _ = StartLifetimeServicesAsync(desktop);
        }

        // macOS: Dock 栏点击重新打开主窗口
        if (Application.Current?.TryGetFeature<IActivatableLifetime>() is { } activatable)
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
            // 循环引用保护
            visited ??= [];
            if (!visited.Add(exception))
            {
                break;
            }

            // HttpRequestException(含 SSL/认证失败)、SocketException、AuthenticationException 均视为传输层问题
            // 不纳入 IOException，避免本地文件系统异常（文件缺失/损坏等）被误判为网络问题而吞掉
            if (
                exception is HttpRequestException
                or SocketException
                or AuthenticationException
            )
            {
                return true;
            }

            // AggregateException: 展开内层异常逐个检查
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

    private static async Task StartLifetimeServicesAsync(
        IClassicDesktopStyleApplicationLifetime desktop
    )
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
            ErrorReporter.Report(
                ex,
                new(
                    ErrorReporter.ErrorReportSource.LifetimeStartup,
                    Phase: "startup",
                    Critical: true,
                    Terminating: true,
                    Level: SentryLevel.Fatal
                )
            );
            Dispatcher.UIThread.Post(() => desktop.Shutdown(-1));
        }
    }

    private static void OnActivated(object? sender, ActivatedEventArgs e)
    {
        if (e.Kind != ActivationKind.Reopen)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        // 已关闭的窗口引用仍留在 MainWindow 属性上，需要判断是否真正可用
        var window = desktop.MainWindow;
        if (window is null || window.IsVisible == false)
        {
            window = ConstructWindow();
            desktop.MainWindow = window;
            window.Show();
        }
        else
        {
            window.Activate();
        }
    }

    internal static Window ConstructWindow()
    {
        if (Program.Services is null)
        {
            return new();
        }

        var configuration = Program.Services.GetRequiredService<ConfigurationService>();
        var window = new MainWindow();

        #region Wire

        var navigation = Program.Services.GetRequiredService<NavigationService>();
        navigation.SetHandler(
            window.Navigate,
            window.GoBack,
            window.CanGoBack,
            window.ClearHistory
        );

        var activator = Program.Services.GetRequiredService<IViewActivator>();
        window.SetFrameActivator(activator);

        var overlay = Program.Services.GetRequiredService<OverlayService>();
        overlay.SetHandler(window.PopToast, window.PopSidebar, window.PopModal, window.PopDialog);

        var notification = Program.Services.GetRequiredService<NotificationService>();
        notification.SetHandler(window.PopGrowl);

        // Unwire：卸载时断开服务 handler，防止悬空引用
        window.Unloaded += (_, _) =>
        {
            navigation.SetHandler(null!, null!, null!, null!);
            overlay.SetHandler(null!, null!, null!, null!);
            notification.SetHandler((Action<GrowlItem>?)null!);
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

        window.Closing += (_, _) =>
        {
            configuration.Value.ApplicationWindowWidth = window.Width;
            configuration.Value.ApplicationWindowHeight = window.Height;
        };

        #endregion

        var themeService = Program.Services.GetRequiredService<ThemeService>();
        window.AttachTheme(themeService);

        // 需要放在整个 window 初始化之后，因为 MainWindowContext 的构造函数要求 window 已与服务绑定
        var viewModel = ActivatorUtilities.CreateInstance<MainWindowContext>(Program.Services);
        window.DataContext = viewModel;

        notification.SetHandler(notification.PopNotification);

        // MainWindowContext 没有 InitializeAsync 能力，这里代替进行初始化
        navigation.Navigate<LandingPage>();

        return window;
    }
}
