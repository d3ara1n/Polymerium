using Avalonia;
using Huskui.Avalonia.Extensions;
using Microsoft.Extensions.Hosting;

namespace Polymerium.App;

internal static class Program
{
    public const string Brand = "Polymerium";

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
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithOutfitFont().LogToTrace();
}