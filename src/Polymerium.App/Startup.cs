using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Extensions;
using Polymerium.App.Services;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Services;

namespace Polymerium.App;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAvalonia()
            .AddLogging()
            .AddHttpClient()
            .ConfigureHttpClientDefaults(builder => builder.RemoveAllLoggers())
            .AddMemoryCache();

        // Trident
        services
            .AddCurseForge()
            .AddPrismLauncher()
            .AddSingleton<ProfileManager>()
            .AddSingleton<RepositoryAgent>()
            .AddSingleton<ImporterAgent>()
            .AddSingleton<InstanceManager>();


        // App
        services
            .AddViewFacilities()
            .AddSingleton<NotificationService>()
            .AddSingleton<NavigationService>()
            .AddSingleton<OverlayService>();
    }
}