using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.Mixins;
using Huskui.Avalonia.Mvvm.Models;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Pages;
using Polymerium.App.Services;
using Sentry;
using Trident.Core.Lifetimes;
using Page = Huskui.Avalonia.Controls.Page;

namespace Polymerium.App;

public class App : Application
{
    public HuskuiTheme? Theme { get; private set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowOrDump(e.ExceptionObject, e.IsTerminating);
        TaskScheduler.UnobservedTaskException += (_, e) => ShowOrDump(e.Exception, !e.Observed);
        Dispatcher.UIThread.UnhandledException += (_, e) => ShowOrDump(e.Exception, !e.Handled);

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
            ShowOrDump(ex, true);
            desktop.Shutdown(-1);
        }
    }

    private static void ShowOrDump(object core, bool critical = false)
    {
        // 只接受致命错误的 dump，避免 TaskCancellationException 等在 Task 中发生触发 UnobservedTaskException 转发把 Dump 吃满
        if (!critical)
        {
            return;
        }

        if (core is Exception rec)
        {
            SentrySdk.CaptureException(rec);
        }

        if (core is Exception ex && !critical && Program.Services?.GetService<NavigationService>() is { } navigation)
        {
            Dispatcher.UIThread.Post(() => navigation.Navigate<ExceptionPage>(ex));
        }
        else
        {
            Dump(core);
        }
    }

    private static void Dump(object core)
    {
        // 只有调试模式才转储错误报告，而 Prod 模式有大概率文件目录是只读的
        if (!Program.IsDebug)
            return;

        var path = Path.Combine(AppContext.BaseDirectory, "dumps", $"Exception-{DateTimeOffset.Now.ToFileTime()}.log");
        var sb = new StringBuilder($"""
                                    // {DateTimeOffset.Now.ToString()}
                                    // Polymerium: {typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                                                  ?.InformationalVersion.Split('+')[0] ?? Program.Version}
                                    // Avalonia: {Assembly.GetEntryAssembly()?.GetName().Version}

                                    """);
        sb.AppendLine();
        DumpInternal(sb, core, 0);
        var dir = Path.GetDirectoryName(path);
        if (dir is not null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static void DumpInternal(StringBuilder builder, object core, int level)
    {
        switch (core)
        {
            case AggregateException ae:
                builder.AppendLine($"""
                                    --- LEVEL: {level} ---
                                    Exception: {ae.GetType().Name}
                                    Message: {ae.Message}
                                    StackTrace: {ae.StackTrace}

                                    """);
                foreach (var inner in ae.InnerExceptions)
                {
                    DumpInternal(builder, inner, level + 1);
                }

                if (ae.InnerException is not null)
                {
                    DumpInternal(builder, ae.InnerException, level + 1);
                }

                break;
            case Exception e:
                builder.AppendLine($"""
                                    --- LEVEL: {level} ---
                                    Exception: {e.GetType().Name}
                                    Message: {e.Message}
                                    StackTrace: {e.StackTrace}

                                    """);
                if (e.InnerException is not null)
                {
                    DumpInternal(builder, e.InnerException, level + 1);
                }

                break;
            default:
                builder.AppendLine($"""
                                    --- LEVEL: {level} ---
                                    Content: {core.ToString()}

                                    """);
                break;
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
        navigation.SetHandler(window.Navigate, window.GoBack, window.CanGoBack, window.ClearHistory);

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

        // MainWindowContext 没有 InitializeAsync 能力，这里代替进行初始化
        navigation.Navigate<LandingPage>();

        return window;
    }
}
