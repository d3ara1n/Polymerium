using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Huskui.Avalonia;
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
            desktop.MainWindow = ConstructWindow();
            _ = StartLifetimeServicesAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
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
            desktop.Shutdown(-1);
        }
    }

    private static Window ConstructWindow()
    {
        if (Program.Services is null)
        {
            return new();
        }

        var configuration = Program.Services.GetRequiredService<ConfigurationService>();

        var window = new MainWindow();

        // 还原窗口大小
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

        window.SetColorVariant(configuration.Value.ApplicationStyleAccent);
        window.SetThemeVariantByIndex(configuration.Value.ApplicationStyleThemeVariant);
        window.SetTransparencyLevelHintByIndex(configuration.Value.ApplicationStyleBackground);
        window.IsTitleBarVisible = configuration.Value.ApplicationTitleBarVisibility;
        window.IsLeftPanelMode = configuration.Value.ApplicationLeftPanelMode;
        window.SetCornerStyle(configuration.Value.ApplicationStyleCorner);
        // 并不还原窗体大小，没必要

        #region Navigation

        // Link navigation service
        var navigation = Program.Services.GetRequiredService<NavigationService>();
        navigation.SetHandler(
            window.Navigate,
            window.GoBack,
            window.CanGoBack,
            window.ClearHistory
        );

        var activator = Program.Services.GetRequiredService<IViewActivator>();
        window.SetFrameActivator(activator);

        #endregion

        #region Overlay & Notification

        var overlay = Program.Services.GetRequiredService<OverlayService>();
        overlay.SetHandler(window.PopToast, window.PopSidebar, window.PopModal, window.PopDialog);
        var notification = Program.Services.GetRequiredService<NotificationService>();
        notification.SetHandler(window.PopGrowl);

        #endregion

        // 需要放在整个 window 初始化之后，因为 MainWindowContext 的构造函数要求 window 已与服务绑定
        var viewModel = ActivatorUtilities.CreateInstance<MainWindowContext>(Program.Services);
        window.DataContext = viewModel;
        notification.SetHandler(viewModel.PopNotification);

        // 应用级 NativeMenu 的 DataContext 设为同一个 ViewModel，使菜单命令绑定生效
        Application.Current!.DataContext = viewModel;

        // MainWindowContext 没有 InitializeAsync 能力，这里代替进行初始化
        navigation.Navigate<LandingPage>();

        return window;
    }
}
