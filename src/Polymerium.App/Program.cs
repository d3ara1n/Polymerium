using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
     ?? typeof(Program).Assembly.GetName().Version?.ToString() ?? "Eternal";

    public static readonly string MagicWords = "say u say me";

    public static readonly string MirrorChyanCdk = "0001bf520b5a75eb3e61f458";

    private static Action? exitAction;

    internal static IHost? AppHost { get; private set; }

    public static bool Debug { get; private set; } = Debugger.IsAttached;
    public static bool FirstRun { get; private set; }

    public static void Main(string[] args)
    {
        VelopackApp.Build().OnFirstRun(_ => FirstRun = true).Run();

        #region Before lifetime configuration

        var overrideFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                        ".trident.home");
        if (File.Exists(overrideFile))
        {
            var firstLine = File.ReadLines(overrideFile).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstLine) && Path.IsPathRooted(firstLine) && !File.Exists(firstLine))
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

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            EnvironmentName = Debug ? "Development" : "Production"
        });
        Startup.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);
        Debug = Debug || builder.Environment.EnvironmentName == "Development";
        AppHost = builder.Build();

        if (OperatingSystem.IsMacOS())
        {
            ConfigureDesktopRuntime(AppHost.Services);
            AppHost.StartAsync().GetAwaiter().GetResult();

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                AppHost.StopAsync().GetAwaiter().GetResult();
                exitAction?.Invoke();
            }

            return;
        }

        AppHost.Run();

        exitAction?.Invoke();
    }

    public static void Terminate(Action? beforeDie)
    {
        exitAction = beforeDie;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private static void ConfigureDesktopRuntime(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<ConfigurationService>();
        var environment = services.GetRequiredService<IHostEnvironment>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("AvaloniaLifetime");

        logger.LogInformation("""
                              {app}({env}):{root}
                              Polymerium/{app_version}
                              Avalonia({debug})/{ava_version}
                              Home: {home}
                              """,
                              environment.ApplicationName,
                              environment.EnvironmentName,
                              environment.ContentRootPath,
                              typeof(Program).Assembly.GetName().Version,
                              Debug ? "Debug" : "Prod",
                              typeof(AvaloniaObject).Assembly.GetName().Version,
                              PathDef.Default.Home);

        CultureInfo.CurrentUICulture = GetSafeCultureInfo(configuration.Value.ApplicationLanguage);
        Resources.Culture = CultureInfo.CurrentUICulture;
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

        if (Debug)
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
