using System;
using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Microsoft.Extensions.Hosting;
using Velopack;

namespace Polymerium.App;

internal static class Program
{
    public static readonly string Brand = "Polymerium";

    public static readonly string Version = typeof(Program).Assembly
                                                .GetCustomAttribute<
                                                    AssemblyInformationalVersionAttribute>()
                                                ?.InformationalVersion
                                            ?? typeof(Program).Assembly.GetName().Version?.ToString() ?? "Eternal";

    // // Initialization code. Don't use any Avalonia, third-party APIs or any
    // // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // // yet and stuff might break.
    //[STAThread]
    //public static void Main(string[] args)
    //{
    //    BuildAvaloniaApp()
    //        .StartWithClassicDesktopLifetime(args);
    //}

    internal static IHost? AppHost { get; private set; }

    public static bool Debug { get; private set; } = Debugger.IsAttached;
    public static bool FirstRun { get; private set; }

    public static void Main(string[] args)
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            VelopackApp.Build().OnFirstRun(_ => FirstRun = true).Run();

        var builder = Host.CreateApplicationBuilder(args);
        Startup.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);
        AppHost = builder.Build();
        Debug = Debug || builder.Environment.EnvironmentName == "Development";
        AppHost.Run();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>().UsePlatformDetect().WithFontSetup();

        if (Debug)
            builder.LogToTextWriter(Console.Out);
        else
            builder.LogToTrace();

        return builder;
    }
}