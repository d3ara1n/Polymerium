using Avalonia;
using Avalonia.Media;
using FluentIcons.Avalonia;
using Huskui.Avalonia.Extensions;
using Microsoft.Extensions.Hosting;

namespace Polymerium.App;

internal static class Program
{
    // // Initialization code. Don't use any Avalonia, third-party APIs or any
    // // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // // yet and stuff might break.
    // [STAThread]
    // public static void Main(string[] args)
    // {
    //     BuildAvaloniaApp()
    //         .StartWithClassicDesktopLifetime(args);
    // }

    internal static IHost? AppHost { get; private set; }

    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();
        Startup.ConfigureServices(builder.Services, builder.Configuration);
        AppHost = builder.Build();
        AppHost.Run();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithOutfitFont()
            .UseSegoeMetrics()
            .With(new RenderOptions
            {
                EdgeMode = EdgeMode.Antialias,
                TextRenderingMode = TextRenderingMode.SubpixelAntialias
            })
            .LogToTrace();
    }
}