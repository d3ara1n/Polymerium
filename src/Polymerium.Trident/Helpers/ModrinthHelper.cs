using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Polymerium.Trident.Helpers;

public static class ModrinthHelper
{
    public const string ENDPOINT = "https://api.modrinth.com/v3";

    public const string MODRINTH_URL = "";

    private static readonly JsonSerializerOptions OPTIONS = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static async Task<T?> GetResourceAsync<T>(ILogger logger, IHttpClientFactory factory, string service,
        CancellationToken token = default)
        where T : struct
    {
        if (token.IsCancellationRequested) return null;
        using var client = factory.CreateClient();
        try
        {
            var result = await client.GetFromJsonAsync<T>(ENDPOINT + service, OPTIONS, token);
            return result;
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed to get {} from Modrinth for {}", service, e.Message);
            return null;
        }
    }
}