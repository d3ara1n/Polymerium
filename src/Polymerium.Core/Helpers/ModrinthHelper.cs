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
using Polymerium.Core.Models.Modrinth;
using Wupoo;

namespace Polymerium.Core.Helpers;

public static class ModrinthHelper
{
    private const string ENDPOINT = "https://api.modrinth.com/v2";

    private static async Task<Option<T>> GetResourceAsync<T>(string service, CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return Option<T>.None();
        T? result = default;
        var found = false;
        await Wapoo.Wohoo(ENDPOINT + service)
            .ForJsonResult<T>(x =>
            {
                result = x;
                found = true;
            })
            .ViaGet()
            .FetchAsync(token);
        return found ? Option<T>.Some(result!) : Option<T>.None();
    }

    private static async Task<IEnumerable<T>> GetResourcesAsync<T>(string service,
        CancellationToken token = default)
    {
        IEnumerable<T>? results = null;
        await Wapoo.Wohoo(ENDPOINT + service)
            .ForJsonResult<JObject>(x =>
            {
                if (x.ContainsKey("hits")) results = x["hits"]!.ToObject<IEnumerable<T>>();
            })
            .ViaGet()
            .FetchAsync(token);
        return results ?? Enumerable.Empty<T>();
    }

    public static async Task<Option<ModrinthProject>> GetProjectAsync(string id, CancellationToken token = default)
    {
        var service = $"/project/{id}";
        return await GetResourceAsync<ModrinthProject>(service, token);
    }

    public static async Task<IEnumerable<ModrinthHit>> SearchProjectsAsync(string query, ResourceType type,
        string? gameVersion = null, string? modLoaderId = null, uint offset = 0, uint limit = 10,
        CancellationToken token = default)
    {
        var facets = new List<KeyValuePair<string, string>>();
        if (gameVersion != null) facets.Add(new KeyValuePair<string, string>("version", gameVersion));

        if (modLoaderId != null)
            facets.Add(new KeyValuePair<string, string>("categories", modLoaderId switch
            {
                ComponentMeta.FORGE => "forge",
                ComponentMeta.FABRIC => "fabric",
                ComponentMeta.QUILT => "quilt",
                _ => throw new NotSupportedException()
            }));

        facets.Add(new KeyValuePair<string, string>("project_type", type switch
        {
            ResourceType.Modpack => "modpack",
            ResourceType.Shader => "shader",
            ResourceType.Mod => "mod",
            ResourceType.Plugin => "mod",
            ResourceType.DataPack => "mod",
            ResourceType.ResourcePack => "resourcepack",
            _ => throw new NotImplementedException()
        }));
        var service =
            $"/search?query={HttpUtility.UrlEncode(query)}&offset={offset}&limit={limit}&facets=[{string.Join(',', facets.Select(x => $"[\"{x.Key}:{x.Value}\"]"))}]";
        return await GetResourcesAsync<ModrinthHit>(service, token);
    }
}