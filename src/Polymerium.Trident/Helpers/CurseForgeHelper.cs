using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Models.Eternal;
using System.Net.Http.Json;
using System.Web;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Helpers
{
    public static class CurseForgeHelper
    {
        private const string API_KEY = "$2a$10$cjd5uExXA6oMi3lSnylNC.xsFJiujI8uQ/pV1eGltFe/hlDO2mjzm";
        private const string ENDPOINT = "https://api.curseforge.com/v1";

        private const uint GAME_ID = 432;
        private const uint CLASSID_MODPACK = 4471;
        private const uint CLASSID_MOD = 6;
        private const uint CLASSID_WORLD = 17;
        private const uint CLASSID_RESOURCEPACK = 12;

        public static readonly IReadOnlyDictionary<string, string> MODLOADERS_MAPPINGS = new Dictionary<string, string>
        {
            { "Forge", Profile.Loader.COMPONENT_FORGE },
            { "NeoForge", Profile.Loader.COMPONENT_NEOFORGE },
            { "Fabric", Profile.Loader.COMPONENT_FABRIC },
            { "Quilt", Profile.Loader.COMPONENT_QUILT }
        }.AsReadOnly();

        public class ResponseWrapper<T>
        {
            public T? Data { get; set; }
        }

        public static ResourceKind GetResourceTypeFromClassId(uint classId)
        {
            return classId switch
            {
                6 => ResourceKind.Mod,
                12 => ResourceKind.ResourcePack,
                17 => ResourceKind.World,
                4546 => ResourceKind.ShaderPack,
                4471 => ResourceKind.Modpack,
                6552 => ResourceKind.ShaderPack,
                _ => throw new NotImplementedException()
            };
        }

        private static async Task<T?> GetResourceAsync<T>(ILogger logger, IHttpClientFactory factory, string service, IMemoryCache cache, CancellationToken token = default)
        where T : struct
        {
            if (token.IsCancellationRequested)
                return null;
            return await cache.GetOrCreateAsync(
                service,
                async entry =>
                {
                    var found = false;
                    T? result = default;
                    var client = factory.CreateClient();
                    client.DefaultRequestHeaders.Add("x-api-key", API_KEY);
                    try
                    {
                        var response = await client.GetFromJsonAsync<ResponseWrapper<T>>(ENDPOINT + service, token);
                        if (response?.Data != null)
                        {
                            result = response.Data;
                            found = true;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogWarning("Failed to get {} from CurseForge for {}", service, e.Message);
                    }
                    entry.SetSlidingExpiration(TimeSpan.FromSeconds(found ? 60 * 60 : 1));
                    return result;
                }
            );
        }

        public static async Task<IEnumerable<T>> GetResourcesAsync<T>(ILogger logger, IHttpClientFactory factory, string service, IMemoryCache cache, CancellationToken token = default)
            where T : struct
        {
            if (token.IsCancellationRequested)
                return Enumerable.Empty<T>();
            return await cache.GetOrCreateAsync(
                    service,
                    async entry =>
                    {
                        IEnumerable<T>? results = null;
                        var client = factory.CreateClient();
                        client.DefaultRequestHeaders.Add("x-api-key", API_KEY);
                        try
                        {
                            var response = await client.GetFromJsonAsync<ResponseWrapper<IEnumerable<T>>>(ENDPOINT + service, token);
                            if (response?.Data != null)
                            {
                                results = response.Data;
                            }
                        }
                        catch (Exception e)
                        {
                            logger.LogWarning("Failed to get {} from CurseForge for {}", service, e.Message);
                        }
                        entry.SetSlidingExpiration(TimeSpan.FromSeconds(results != null ? 60 * 60 : 1));
                        return results ?? Enumerable.Empty<T>();
                    }
                ) ?? Enumerable.Empty<T>();
        }

        public static async Task<IEnumerable<EternalProject>> SearchProjectsAsync(ILogger logger, IHttpClientFactory factory, IMemoryCache cache, string query, ResourceKind kind, string? gameVersion = null, string? modLoaderId = null, uint offset = 0, uint limit = 10, CancellationToken token = default)
        {
            var modLoaderType = modLoaderId switch
            {
                Profile.Loader.COMPONENT_FORGE => 1,
                Profile.Loader.COMPONENT_FABRIC => 4,
                Profile.Loader.COMPONENT_QUILT => 5,
                _ => 0
            };
            var service =
                $"/mods/search?gameId={GAME_ID}&classId={kind switch
                {
                    ResourceKind.Modpack => CLASSID_MODPACK,
                    ResourceKind.Mod => CLASSID_MOD,
                    ResourceKind.ResourcePack => CLASSID_RESOURCEPACK,
                    ResourceKind.World => CLASSID_WORLD,
                    _ => throw new NotSupportedException()
                }}&index={offset}&pageSize={limit}&searchFilter={HttpUtility.UrlPathEncode(query)}&sortField=2&sortOrder=desc"
                + (gameVersion != null ? $"&gameVersion={gameVersion}" : "")
                + (
                    (kind == ResourceKind.Mod || kind == ResourceKind.Modpack) && modLoaderId != null
                        ? $"&modLoaderType={modLoaderType}"
                        : ""
                );
            return await GetResourcesAsync<EternalProject>(logger, factory, service, cache, token);
        }
    }
}
