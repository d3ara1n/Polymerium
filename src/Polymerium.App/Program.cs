﻿using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Logging;
using Microsoft.Extensions.Hosting;

namespace Polymerium.App;

internal static class Program
{
    public static readonly string Brand = "Polymerium";
    public static readonly string Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "Eternal";

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

    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        Startup.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);
        AppHost = builder.Build();
        AppHost.Run();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>().UsePlatformDetect().WithFontSetup();

        if (Debug)
            builder.LogToTextWriter(Console.Out, LogEventLevel.Debug);
        else
            builder.LogToTrace();

        return builder;
    }

    public static bool Debug { get; } = Environment.GetEnvironmentVariable("POLYMERIUM_DEBUG") != null
                                     || Debugger.IsAttached;
}