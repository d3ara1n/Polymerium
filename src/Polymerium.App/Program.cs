using Avalonia;
using System;
using Huskui.Avalonia.Extensions;
using Microsoft.Extensions.Hosting;

namespace Polymerium.App;

internal static class Program
{
#if DEBUG
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
#else
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();
        Startup.ConfigureServices(builder.Services, builder.Configuration);
        var host = builder.Build();
        host.Run();
    }
#endif
    
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithOutfitFont()
            .LogToTrace();
}