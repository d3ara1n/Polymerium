using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Properties;
using Polymerium.App.Services;
using Trident.Abstractions;
using Velopack;

namespace Polymerium.App;

internal static class Program
{
    public static readonly string Brand = "Polymerium";

    public static readonly string Version =
        typeof(Program)
            .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0]
        ?? typeof(Program).Assembly.GetName().Version?.ToString()
        ?? "Eternal";

    public static readonly string MagicWords = "say u say me";

    public static readonly string MirrorChyanCdk = "0001bf520b5a75eb3e61f458";

    private static Action? exitAction;

    internal static IServiceProvider? Services { get; private set; }

    public static bool FirstRun { get; private set; }

#if DEBUG
    public static bool IsDebug => true;
#else
    public static bool IsDebug { get; } =
        Debugger.IsAttached
        || Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") != "Production";
#endif

    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().OnFirstRun(_ => FirstRun = true).Run();

        #region Before lifetime configuration

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

        Startup.InitializeUnhostedServices();

        var configurationService = Services.GetRequiredService<ConfigurationService>();
        CultureInfo.CurrentUICulture = GetSafeCultureInfo(
            configurationService.Value.ApplicationLanguage
        );
        Resources.Culture = CultureInfo.CurrentUICulture;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        Startup.DeinitializeUnhostedServices();
        ((IDisposable)Services).Dispose();
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
        var builder = AppBuilder.Configure<App>().UsePlatformDetect().WithFontSetup();

        if (IsDebug)
        {
            builder.LogToTextWriter(Console.Out);
        }
        else
        {
            builder.LogToTrace();
        }

        return builder;
    }
}
