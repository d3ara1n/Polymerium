using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polymerium.App.Services;

namespace Polymerium.App.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddAvalonia(this IServiceCollection services)
    {
        services.AddSingleton<IHostLifetime, AvaloniaLifetime>();
    }
}