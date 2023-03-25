using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Components;
using Polymerium.Core.Models.CurseForge.Eternal;
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

    public static readonly IReadOnlyDictionary<string, string> MODLOADERS_MAPPINGS = new Dictionary<string, string>
    {
        { "Forge", ComponentMeta.FORGE },
        { "Fabric", ComponentMeta.FABRIC },
        { "Quilt", ComponentMeta.QUILT }
    }.AsReadOnly();

    private static async Task<Option<T>> GetResourceAsync<T>(string service,
        CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return Option<T>.None();
        var found = false;
        T? result = default;
        await Wapoo.Wohoo(ENDPOINT + service)
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
        return found ? Option<T>.Some(result!) : Option<T>.None();
    }

    public static async Task<IEnumerable<T>> GetResourcesAsync<T>(string service, CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return Enumerable.Empty<T>();
        IEnumerable<T>? results = null;
        await Wapoo.Wohoo(ENDPOINT + service)
            .WithHeader("x-api-key", API_KEY)
            .ForJsonResult<JObject>(x =>
            {
                if (x.ContainsKey("data")) results = x["data"]!.ToObject<IEnumerable<T>>();
            })
            .FetchAsync();
        return results != null ? results : Enumerable.Empty<T>();
    }

    public static async Task<IEnumerable<EternalProject>> SearchProjectsAsync(string query, ResourceType type,
        string? gameVersion = null, string? modLoaderId = null, uint offset = 0, uint limit = 10,
        CancellationToken token = default)
    {
        var service = $"/mods/search?gameId={GAME_ID}&classId={type switch
        {
            ResourceType.Modpack => CLASSID_MODPACK,
            ResourceType.Mod => CLASSID_MOD,
            ResourceType.ResourcePack => CLASSID_RESOURCEPACK,
            ResourceType.World => CLASSID_WORLD,
            _ => throw new NotSupportedException()
        }}&index={offset}&pageSize={limit}&searchFilter={HttpUtility.UrlEncode(query)}&sortField=2&sortOrder=desc"
                      + (gameVersion != null ? $"&gameVersion={gameVersion}" : "")
                      + ((type == ResourceType.Mod || type == ResourceType.Modpack) && modLoaderId != null
                          ? $"&modLoaderType={modLoaderId switch
                          {
                              ComponentMeta.FORGE => 1,
                              ComponentMeta.FABRIC => 4,
                              ComponentMeta.QUILT => 5,
                              _ => 0
                          }}"
                          : "");
        return await GetResourcesAsync<EternalProject>(service, token);
    }

    public static async Task<Option<EternalProject>> GetModInfoAsync(uint projectId, CancellationToken token = default)
    {
        var service = $"/mods/{projectId}";
        return await GetResourceAsync<EternalProject>(service, token);
    }

    public static async Task<Option<string>> GetModDescriptionAsync(uint projectId, CancellationToken token = default)
    {
        var service = $"/mods/{projectId}/description";
        return await GetResourceAsync<string>(service, token);
    }

    public static async Task<Option<string>> GetModDownloadUrlAsync(uint projectId, int fileId,
        CancellationToken token = default)
    {
        var service = $"/mods/{projectId}/files/{fileId}/download-url";
        return await GetResourceAsync<string>(service, token);
    }

    public static async Task<Option<EternalModFile>> GetModFileInfoAsync(uint projectId, uint fileId,
        CancellationToken token = default)
    {
        var service = $"/mods/{projectId}/files/{fileId}";
        return await GetResourceAsync<EternalModFile>(service, token);
    }

    public static async Task<IEnumerable<EternalModFile>> GetModFilesAsync(uint projectId,
        CancellationToken token = default)
    {
        var service = $"/mods/{projectId}/files";
        return await GetResourcesAsync<EternalModFile>(service, token);
    }
}