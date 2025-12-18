using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MirrorChyan.Net.Clients;
using MirrorChyan.Net.Services;
using Refit;

namespace MirrorChyan.Net;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMirrorChyan(
            string productId,
            string clientName,
            string currentVersion,
            Uri? baseAddress = null)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            services
               .AddRefitClient<IMirrorChyanClient>(_ => new(new SystemTextJsonContentSerializer(options)))
               .ConfigureHttpClient((sp, client) =>
                {
                    var o = sp.GetRequiredService<IOptionsMonitor<MirrorChyanOptions>>();
                    client.BaseAddress = o.CurrentValue.BaseAddress;
                });
            services
               .AddSingleton<IMirrorChyanService, MirrorChyanService>()
               .Configure<MirrorChyanOptions>(o =>
                {
                    o.ProductId = productId;
                    o.ClientName = clientName;
                    o.VersionString = currentVersion;
                    if (baseAddress is not null)
                        o.BaseAddress = baseAddress;
                });
            return services;
        }
    }
}
