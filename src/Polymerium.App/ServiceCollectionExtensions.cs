using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Facilities;
using Polymerium.App.Services;

namespace Polymerium.App;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAvalonia(this IServiceCollection services)
    {
        services.AddHostedService<AvaloniaLifetime>();
        return services;
    }

    public static IServiceCollection AddViewFacilities(this IServiceCollection services)
    {
        services.AddScoped<ViewBagFactory>().AddScoped<ViewBag>();
        return services;
    }
}