using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Polymerium.Trident.Helpers
{
    public static class ModrinthHelper
    {
        public const string ENDPOINT = "https://api.modrinth.com/v3";

        public const string MODRINTH_URL = "";

        private static readonly JsonSerializerOptions OPTIONS = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public static async Task<T> GetResourceAsync<T>(ILogger logger, IHttpClientFactory factory, string service,
            CancellationToken token = default)
            where T : struct
        {
            token.ThrowIfCancellationRequested();
            using var client = factory.CreateClient();
            try
            {
                return await client.GetFromJsonAsync<T>(ENDPOINT + service, OPTIONS, token);
            }
            catch (Exception e)
            {
                logger.LogWarning("Failed to get {} from Modrinth for {}", service, e.Message);
                throw;
            }
        }
    }
}