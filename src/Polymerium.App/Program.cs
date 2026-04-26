using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using AsyncImageLoader;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Huskui.Avalonia.Mvvm.States;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Properties;
using Polymerium.App.Services;
using Sentry;
using Trident.Abstractions;
using Trident.Core.Lifetimes;
using Velopack;

namespace Polymerium.App;

internal static class Program
{
    public static readonly string Brand = "Polymerium";

    public static readonly string Version = GitVersionInformation.SemVer;

    public static readonly string ReleaseDate = GitVersionInformation.CommitDate;

    public static readonly string MagicWords = "say u say me";

    public static readonly string MirrorChyanCdk = "0001bf520b5a75eb3e61f458";

    private static Action? exitAction;

    internal static IServiceProvider? Services { get; private set; }

    public static bool FirstRun { get; private set; }

#if DEBUG
    public static bool IsDebug => true;
#else
    public static bool IsDebug => false;
#endif

    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().OnFirstRun(_ => FirstRun = true).Run();

        #region 0. 这些设置需要在整个应用启动的第一时间完成

        Startup.InitializeUnhostedServices();

        var overrideFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".trident.home"
        );
        if (File.Exists(overrideFile))
        {
            var firstLine = File.ReadLines(overrideFile).FirstOrDefault();
            if (
                !string.IsNullOrWhiteSpace(firstLine)
                && Path.IsPathRooted(firstLine)
                && !File.Exists(firstLine)
            )
            {
                PathDef.Default = new(firstLine);
            }
        }

        var firstRunFile = Path.Combine(PathDef.Default.PrivateDirectory(Brand), "first_run");
        if (!File.Exists(firstRunFile))
        {
            FirstRun = true;
            var dir = Path.GetDirectoryName(firstRunFile);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(firstRunFile, MagicWords);
        }

        #endregion


        var services = new ServiceCollection();
        Startup.ConfigureServices(services, IsDebug);
        Services = services.BuildServiceProvider();

        #region 1. 服务已构造，现在初始化这些服务

        var configurationService = Services.GetRequiredService<ConfigurationService>();
        CultureInfo.CurrentUICulture = GetSafeCultureInfo(
            configurationService.Value.ApplicationLanguage
        );
        Resources.Culture = CultureInfo.CurrentUICulture;
        var httpClient = Services.GetRequiredService<HttpClient>();
        var loader = new SuppressedImageLoader(httpClient);
        ImageLoader.AsyncImageLoader = loader;
        ImageBrushLoader.AsyncImageLoader = loader;

        #endregion

        #region 2. Avalonia 启动窗口之后调用部分耗时服务

        // PROCEDURE MOVED: Lifetime Services 在 App.OnFrameworkInitialized 中进行延迟初始化而不是 Program 收尾

        #endregion

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        #region 7. Avalonia 退出之后处置部分耗时服务

        Exception? stopException = null;
        if (Services.GetService<LifetimeServiceRuntime>() is { } runtime)
        {
            try
            {
                runtime.StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                stopException = ex;
            }
        }

        #endregion


        #region 8. 处置整个服务容器

        try
        {
            #region 8.1 部分用 ServiceProvider.Dispose 不可靠的服务处置需要显式写在这

            Services.GetRequiredService<IViewStateStore>().Flush();

            #endregion
        }
        finally
        {
            #region 8.2 杂七杂八的服务处置

            ((IDisposable)Services).Dispose();

            #endregion
        }

        if (stopException is not null)
        {
            ErrorReporter.Report(
                stopException,
                new(
                    ErrorReporter.ErrorReportSource.LifetimeShutdown,
                    Phase: "shutdown",
                    Critical: true,
                    Terminating: true,
                    Level: SentryLevel.Error
                )
            );
        }

        #endregion

        #region 9. 这些服务与应用程序生命周期无关且不影响，放在最后进行

        Startup.DeinitializeUnhostedServices();

        #endregion

        exitAction?.Invoke();
    }

    public static void Terminate(Action? beforeDie)
    {
        exitAction = beforeDie;
        if (
            Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop
        )
        {
            desktop.Shutdown();
        }
    }

    private static CultureInfo GetSafeCultureInfo(string cultureName)
    {
        try
        {
            return CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("en-US");
        }
        catch (ArgumentException)
        {
            return CultureInfo.GetCultureInfo("en-US");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder
            .Configure<App>()
#if DEBUG
            .WithDeveloperTools()
            .LogToTextWriter(Console.Out)
#else
        .LogToTrace()
#endif
            .UsePlatformDetect()
            .WithFontSetup();

        return builder;
    }
}
