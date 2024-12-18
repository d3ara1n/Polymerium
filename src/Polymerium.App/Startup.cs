using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Extensions;
using Polymerium.App.Services;
using Polymerium.Trident.Services;

namespace Polymerium.App;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAvalonia()
            .AddLogging();

        // Trident
        services
            .AddSingleton<ProfileService>()
            .AddSingleton<RepositoryAgent>();

        // App
        services
            .AddViewFacilities()
            .AddSingleton<NavigationService>();
    }
}