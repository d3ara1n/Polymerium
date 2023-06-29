using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Components;
using Polymerium.Core.Models.CurseForge.Eternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Wupoo;

namespace Polymerium.Core.Helpers;

public static class CurseForgeHelper
{
    // NOTE: CurseForge-ApiKey 注册账号就有，再分发前请替换 ApiKey
    // TODO: Api key 都应该自 Configuration/Option 模式输入
    private const string API_KEY = "$2a$10$cjd5uExXA6oMi3lSnylNC.xsFJiujI8uQ/pV1eGltFe/hlDO2mjzm";
    private const string ENDPOINT = "https://api.curseforge.com/v1";

    private const uint GAME_ID = 432;
    private const uint CLASSID_MODPACK = 4471;
    private const uint CLASSID_MOD = 6;
    private const uint CLASSID_WORLD = 17;
    private const uint CLASSID_RESOURCEPACK = 12;

    public static readonly IReadOnlyDictionary<string, string> MODLOADERS_MAPPINGS = new Dictionary<
        string,
        string
    >
    {
        { "Forge", ComponentMeta.FORGE },
        { "Fabric", ComponentMeta.FABRIC },
        { "Quilt", ComponentMeta.QUILT }
    }.AsReadOnly();

    public static Uri MakeResourceUrl(ResourceType type, string projectId, string versionId)
    {
        return type switch
        {
            ResourceType.Update
                => new Uri($"poly-res://curseforge@update/{projectId}?current={versionId}"),
            ResourceType.File => new Uri($"poly-res://curseforge@file/{projectId}/{versionId}"),
            _
                => new Uri(
                    $"poly-res://curseforge@{type.ToString().ToLower()}/{projectId}?version={versionId}"
                )
        };
    }

    public static ResourceType GetResourceTypeFromClassId(uint classId)
    {
        return classId switch
        {
            6 => ResourceType.Mod,
            12 => ResourceType.ResourcePack,
            17 => ResourceType.World,
            4546 => ResourceType.ShaderPack,
            4471 => ResourceType.Modpack,
            _ => throw new NotImplementedException()
        };
    }

    private static async Task<T?> GetResourceAsync<T>(
        string service,
        IMemoryCache cache,
        CancellationToken token = default
    )
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
                await Wapoo
                    .Wohoo(ENDPOINT + service)
                    .WithHeader("x-api-key", API_KEY)
                    .ForJsonResult<JObject>(x =>
                    {
                        if (x.ContainsKey("data"))
                        {
                            result = x["data"]!.ToObject<T>();
                            found = true;
                        }
                    })
                    .FetchAsync();
                entry.SetSlidingExpiration(TimeSpan.FromSeconds(found ? 60 * 60 : 1));
                return result;
            }
        );
    }

    private static async Task<string?> GetStringResourceAsync(
        string service,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        if (token.IsCancellationRequested)
            return null;
        return await cache.GetOrCreateAsync(
            service,
            async entry =>
            {
                var found = false;
                string? result = null;
                await Wapoo
                    .Wohoo(ENDPOINT + service)
                    .WithHeader("x-api-key", API_KEY)
                    .ForJsonResult<JObject>(x =>
                    {
                        if (x.ContainsKey("data"))
                        {
                            result = x.Value<string>("data");
                            found = true;
                        }
                    })
                    .FetchAsync();
                entry.SetSlidingExpiration(TimeSpan.FromSeconds(found ? 60 * 60 : 1));
                return result;
            }
        );
    }

    public static async Task<IEnumerable<T>> GetResourcesAsync<T>(
        string service,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        if (token.IsCancellationRequested)
            return Enumerable.Empty<T>();
        return await cache.GetOrCreateAsync(
                service,
                async entry =>
                {
                    IEnumerable<T>? results = null;
                    await Wapoo
                        .Wohoo(ENDPOINT + service)
                        .WithHeader("x-api-key", API_KEY)
                        .ForJsonResult<JObject>(x =>
                        {
                            if (x.ContainsKey("data"))
                                results = x["data"]!.ToObject<IEnumerable<T>>();
                        })
                        .FetchAsync();
                    entry.SetSlidingExpiration(TimeSpan.FromSeconds(results != null ? 60 * 60 : 1));
                    return results ?? Enumerable.Empty<T>();
                }
            ) ?? Enumerable.Empty<T>();
    }

    public static async Task<IEnumerable<EternalProject>> SearchProjectsAsync(
        IMemoryCache cache,
        string query,
        ResourceType type,
        string? gameVersion = null,
        string? modLoaderId = null,
        uint offset = 0,
        uint limit = 10,
        CancellationToken token = default
    )
    {
        var modLoaderType = modLoaderId switch
        {
            ComponentMeta.FORGE => 1,
            ComponentMeta.FABRIC => 4,
            ComponentMeta.QUILT => 5,
            _ => 0
        };
        var service =
            $"/mods/search?gameId={GAME_ID}&classId={type switch
            {
                ResourceType.Modpack => CLASSID_MODPACK,
                ResourceType.Mod => CLASSID_MOD,
                ResourceType.ResourcePack => CLASSID_RESOURCEPACK,
                ResourceType.World => CLASSID_WORLD,
                _ => throw new NotSupportedException()
            }}&index={offset}&pageSize={limit}&searchFilter={HttpUtility.UrlEncode(query)}&sortField=2&sortOrder=desc"
            + (gameVersion != null ? $"&gameVersion={gameVersion}" : "")
            + (
                (type == ResourceType.Mod || type == ResourceType.Modpack) && modLoaderId != null
                    ? $"&modLoaderType={modLoaderType}"
                    : ""
            );
        return await GetResourcesAsync<EternalProject>(service, cache, token);
    }

    public static async Task<EternalProject?> GetModInfoAsync(
        uint projectId,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        var service = $"/mods/{projectId}";
        return await GetResourceAsync<EternalProject>(service, cache, token);
    }

    public static async Task<string?> GetModDescriptionAsync(
        uint projectId,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        var service = $"/mods/{projectId}/description";
        return await GetStringResourceAsync(service, cache, token);
    }

    public static async Task<string?> GetModDownloadUrlAsync(
        uint projectId,
        int fileId,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        var service = $"/mods/{projectId}/files/{fileId}/download-url";
        return await GetStringResourceAsync(service, cache, token);
    }

    public static async Task<EternalModFile?> GetModFileInfoAsync(
        uint projectId,
        uint fileId,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        var service = $"/mods/{projectId}/files/{fileId}";
        return await GetResourceAsync<EternalModFile>(service, cache, token);
    }

    public static async Task<IEnumerable<EternalModFile>> GetModFilesAsync(
        uint projectId,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        var service = $"/mods/{projectId}/files";
        return await GetResourcesAsync<EternalModFile>(service, cache, token);
    }
}
