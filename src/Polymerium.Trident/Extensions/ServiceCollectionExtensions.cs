using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polymerium.Trident.Clients;
using Polymerium.Trident.Importers;
using Polymerium.Trident.Repositories;
using Polymerium.Trident.Services;
using Refit;
using Trident.Abstractions.Importers;
using Trident.Abstractions.Repositories;

namespace Polymerium.Trident.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCurseForge(this IServiceCollection services)
    {
        services
           .AddRefitClient<
                ICurseForgeClient>(_ =>
                                       new
                                           RefitSettings(new
                                                             SystemTextJsonContentSerializer(new
                                                                 JsonSerializerOptions(JsonSerializerDefaults
                                                                    .Web))))
           .ConfigureHttpClient(client =>
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

    public static IServiceCollection AddPrismLauncher(this IServiceCollection services)
    {
        services
           .AddRefitClient<
                IPrismLauncherClient>(_ =>
                                          new
                                              RefitSettings(new
                                                                SystemTextJsonContentSerializer(new
                                                                    JsonSerializerOptions(JsonSerializerDefaults
                                                                       .Web))))
           .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(PrismLauncherService.ENDPOINT);
                client.DefaultRequestHeaders.Add("User-Agent",
                                                 $"Polymerium/{Assembly.GetExecutingAssembly().GetName().Version}");
            })
           .AddTransientHttpErrorPolicy(builder => builder.RetryAsync());

        services.AddSingleton<PrismLauncherService>();

        return services;
    }

    public static IServiceCollection AddMojangLauncher(this IServiceCollection services)
    {
        services
           .AddRefitClient<
                IMojangLauncherClient>(_ =>
                                           new
                                               RefitSettings(new
                                                                 SystemTextJsonContentSerializer(new
                                                                     JsonSerializerOptions(JsonSerializerDefaults
                                                                        .Web))))
           .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(MojangLauncherService.ENDPOINT);
                client.DefaultRequestHeaders.Add("User-Agent",
                                                 $"Polymerium/{Assembly.GetExecutingAssembly().GetName().Version}");
            })
           .AddTransientHttpErrorPolicy(builder => builder.RetryAsync());

        services.AddSingleton<MojangLauncherService>();

        return services;
    }

    public static IServiceCollection AddMicrosoft(this IServiceCollection services)
    {
        services
           .AddRefitClient<
                IMicrosoftClient>(_ =>
                                      new RefitSettings(new
                                                            SystemTextJsonContentSerializer(new
                                                                JsonSerializerOptions(JsonSerializerDefaults.Web)
                                                                {
                                                                    PropertyNamingPolicy = JsonNamingPolicy
                                                                       .SnakeCaseLower
                                                                })))
           .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(MicrosoftService.ENDPOINT);
                client.DefaultRequestHeaders.Add("User-Agent",
                                                 $"Polymerium/{Assembly.GetExecutingAssembly().GetName().Version}");
            });
        services.AddSingleton<MicrosoftService>();
        return services;
    }

    public static IServiceCollection AddXboxLive(this IServiceCollection services)
    {
        services
           .AddRefitClient<
                IXboxLiveClient>(_ =>
                                     new RefitSettings(new
                                                           SystemTextJsonContentSerializer(new
                                                               JsonSerializerOptions(JsonSerializerDefaults
                                                                  .General)
                                                               {
                                                                   PropertyNameCaseInsensitive = true
                                                               })))
           .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(XboxLiveService.XBOX_ENDPOINT);
                client.DefaultRequestHeaders.Add("User-Agent",
                                                 $"Polymerium/{Assembly.GetExecutingAssembly().GetName().Version}");
            });
        services
           .AddRefitClient<
                IXboxServiceClient>(_ =>
                                        new
                                            RefitSettings(new
                                                              SystemTextJsonContentSerializer(new
                                                                  JsonSerializerOptions(JsonSerializerDefaults
                                                                     .General)
                                                                  {
                                                                      PropertyNameCaseInsensitive = true
                                                                  })))
           .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(XboxLiveService.XSTS_ENDPOINT);
                client.DefaultRequestHeaders.Add("User-Agent",
                                                 $"Polymerium/{Assembly.GetExecutingAssembly().GetName().Version}");
            });
        services.AddSingleton<XboxLiveService>();
        return services;
    }

    public static IServiceCollection AddMinecraft(this IServiceCollection services)
    {
        services
           .AddRefitClient<
                IMinecraftClient>(_ =>
                                      new RefitSettings(new
                                                            SystemTextJsonContentSerializer(new
                                                                JsonSerializerOptions(JsonSerializerDefaults
                                                                   .Web)
                                                                {
                                                                    PropertyNamingPolicy = JsonNamingPolicy
                                                                       .SnakeCaseLower
                                                                })))
           .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(MinecraftService.ENDPOINT);
                client.DefaultRequestHeaders.Add("User-Agent",
                                                 $"Polymerium/{Assembly.GetExecutingAssembly().GetName().Version}");
            });
        services.AddSingleton<MinecraftService>();
        return services;
    }
}