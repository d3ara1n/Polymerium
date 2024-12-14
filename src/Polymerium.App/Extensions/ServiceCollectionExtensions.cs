using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polymerium.App.Facilities;
using Polymerium.App.Services;

namespace Polymerium.App.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAvalonia(this IServiceCollection services)
    {
        services.AddSingleton<IHostLifetime, AvaloniaLifetime>();
        return services;
    }

    public static IServiceCollection AddViewFacilities(this IServiceCollection services)
    {
        services
            .AddScoped<ViewBagFactory>()
            .AddScoped<ViewBag>();
        return services;
    }
}