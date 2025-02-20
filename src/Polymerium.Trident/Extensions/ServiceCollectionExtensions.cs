using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polymerium.Trident.Clients;
using Polymerium.Trident.Importers;
using Polymerium.Trident.Repositories;
using Polymerium.Trident.Services;
using Refit;
using System.Reflection;
using System.Text.Json;
using Trident.Abstractions.Importers;
using Trident.Abstractions.Repositories;

namespace Polymerium.Trident.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCurseForge(this IServiceCollection services)
    {
        services
            .AddRefitClient<ICurseForgeClient>(_ =>
                new RefitSettings(
                    new SystemTextJsonContentSerializer(
                        new JsonSerializerOptions(JsonSerializerDefaults.Web))))
            .ConfigureHttpClient(
                client =>
                {
                    client.BaseAddress = new Uri(CurseForgeService.ENDPOINT);
                    client.DefaultRequestHeaders.Add("x-api-key", CurseForgeService.API_KEY);
                    client.DefaultRequestHeaders.Add("User-Agent",
                        $"Polymerium/{Assembly.GetExecutingAssembly().GetName().Version}");
                })
            .AddTransientHttpErrorPolicy(builder => builder.RetryAsync());

        services
            .AddSingleton<CurseForgeService>()
            .AddTransient<IRepository, CurseForgeRepository>()
            .AddTransient<IProfileImporter, CurseForgeImporter>();

        return services;
    }
}