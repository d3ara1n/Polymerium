using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Extensions;
using Polymerium.Trident.Services;

namespace Polymerium.App;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAvalonia();

        // Trident
        services.AddSingleton<ProfileService>();
        
        // App
    }
}