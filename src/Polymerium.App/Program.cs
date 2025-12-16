using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Microsoft.Extensions.Hosting;
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
            File.WriteAllText(firstRunFile, MagicWords);
        }

        #endregion

        var builder = Host.CreateApplicationBuilder(args);
        Startup.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);
        Debug = Debug || builder.Environment.EnvironmentName == "Development";
        AppHost = builder.Build();
        AppHost.Run();
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
