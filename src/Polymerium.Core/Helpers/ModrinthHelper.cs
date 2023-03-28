using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Components;
using Polymerium.Core.Models.Modrinth.Labrinth;
using Wupoo;

namespace Polymerium.Core.Helpers;

public static class ModrinthHelper
{
    private const string ENDPOINT = "https://api.modrinth.com/v2";

    public static readonly IReadOnlyDictionary<string, string> MODLOADERS_MAPPINGS = new Dictionary<string, string>
    {
        { "forge", ComponentMeta.FORGE },
        { "fabric", ComponentMeta.FABRIC },
        { "quilt", ComponentMeta.QUILT }
    }.AsReadOnly();

    private static async Task<Option<T>> GetResourceAsync<T>(string service, IMemoryCache cache,
        CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return Option<T>.None();
        return await cache.GetOrCreateAsync<Option<T>>(service, async entry =>
        {
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
            entry.SetSlidingExpiration(TimeSpan.FromSeconds(found ? 60 * 60 : 1));
            return found ? Option<T>.Some(result!) : Option<T>.None();
        }) ?? Option<T>.None();
    }

    private static async Task<IEnumerable<T>> GetResourcesAsync<T>(string service, IMemoryCache cache,
        CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return Enumerable.Empty<T>();
        return await cache.GetOrCreateAsync(service, async entry =>
        {
            IEnumerable<T>? results = null;
            await Wapoo.Wohoo(ENDPOINT + service)
                .ForJsonResult<JArray>(x => { results = x.ToObject<IEnumerable<T>>(); })
                .ViaGet()
                .FetchAsync(token);
            entry.SetSlidingExpiration(TimeSpan.FromSeconds(results != null ? 60 * 60 : 1));
            return results ?? Enumerable.Empty<T>();
        }) ?? Enumerable.Empty<T>();
    }

    private static async Task<IEnumerable<T>> GetHitsAsync<T>(string service, IMemoryCache cache,
        CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return Enumerable.Empty<T>();
        return await cache.GetOrCreateAsync(service, async entry =>
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
        }) ?? Enumerable.Empty<T>();
    }

    public static async Task<Option<LabrinthProject>> GetProjectAsync(string id, IMemoryCache cache,
        CancellationToken token = default)
    {
        var service = $"/project/{id}";
        return await GetResourceAsync<LabrinthProject>(service, cache, token);
    }

    public static async Task<IEnumerable<LabrinthHit>> SearchProjectsAsync(IMemoryCache cache, string query,
        ResourceType type,
        string? gameVersion = null, string? modLoaderId = null, uint offset = 0, uint limit = 10,
        CancellationToken token = default)
    {
        var facets = new List<KeyValuePair<string, string>>();
        if (gameVersion != null) facets.Add(new KeyValuePair<string, string>("versions", gameVersion));

        if (type == ResourceType.Mod && modLoaderId != null)
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
            ResourceType.ShaderPack => "shader",
            ResourceType.Mod => "mod",
            ResourceType.ResourcePack => "resourcepack",
            _ => throw new NotImplementedException()
        }));
        var service =
            $"/search?query={HttpUtility.UrlEncode(query)}&offset={offset}&limit={limit}&facets=[{string.Join(',', facets.Select(x => $"[\"{x.Key}:{x.Value}\"]"))}]";
        return await GetHitsAsync<LabrinthHit>(service, cache, token);
    }

    public static async Task<IEnumerable<LabrinthVersion>> GetProjectVersionsAsync(string projectId, IMemoryCache cache,
        CancellationToken token = default)
    {
        var service = $"/project/{projectId}/version";
        return await GetResourcesAsync<LabrinthVersion>(service, cache, token);
    }

    public static async Task<Option<LabrinthVersion>> GetVersionAsync(string versionId, IMemoryCache cache,
        CancellationToken token = default)
    {
        var service = $"/version/{versionId}";
        return await GetResourceAsync<LabrinthVersion>(service, cache, token);
    }

    public static async Task<IEnumerable<LabrinthTeamMember>> GetTeamMembersAsync(string teamId, IMemoryCache cache,
        CancellationToken token = default)
    {
        var service = $"/team/{teamId}/members";
        return await GetResourcesAsync<LabrinthTeamMember>(service, cache, token);
    }
}