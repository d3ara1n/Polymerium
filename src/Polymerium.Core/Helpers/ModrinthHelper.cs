using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Components;
using Polymerium.Core.Models.Modrinth.Labrinth;
using Wupoo;

namespace Polymerium.Core.Helpers;

public static class ModrinthHelper
{
    private const string ENDPOINT = "https://api.modrinth.com/v2";

    public static readonly IReadOnlyDictionary<string, string> MODLOADERS_MAPPINGS = new Dictionary<
        string,
        string
    >
    {
        { "forge", ComponentMeta.FORGE },
        { "fabric", ComponentMeta.FABRIC },
        { "quilt", ComponentMeta.QUILT }
    }.AsReadOnly();

    public static Uri MakeResourceUrl(
        ResourceType type,
        string projectId,
        string versionId,
        ResourceType? raw
    )
    {
        var dir = (raw ?? type) switch
        {
            ResourceType.Mod => "mods",
            ResourceType.Modpack => "modpacks",
            ResourceType.ResourcePack => "resourcepacks",
            ResourceType.ShaderPack => "shaderpacks",
            ResourceType.DataPack => "datapacks",
            ResourceType.Plugin => "plugins",
            _ => string.Empty
        };
        return type switch
        {
            ResourceType.Update
                => new Uri($"poly-res://modrinth@update/{projectId}?current={versionId}"),
            ResourceType.File => new Uri($"poly-res://modrinth@file/{dir}/{versionId}"),
            _
                => new Uri(
                    $"poly-res://modrinth@{type.ToString().ToLower()}/{projectId}?version={versionId}"
                )
        };
    }

    public static ResourceType GetResourceTypeFromString(string fake)
    {
        return fake switch
        {
            "modpack" => ResourceType.Modpack,
            "shader" => ResourceType.ShaderPack,
            "resourcepack" => ResourceType.ResourcePack,
            // mod, plugin, datapack
            _ => ResourceType.Mod
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
                T? result = null;
                var found = false;
                await Wapoo
                    .Wohoo(ENDPOINT + service)
                    .ForJsonResult<T>(x =>
                    {
                        result = x;
                        found = true;
                    })
                    .ViaGet()
                    .FetchAsync(token);
                entry.SetSlidingExpiration(TimeSpan.FromSeconds(found ? 60 * 60 : 1));
                return result;
            }
        ) ?? null;
    }

    private static async Task<IEnumerable<T>> GetResourcesAsync<T>(
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
                    .ForJsonResult<JArray>(x => { results = x.ToObject<IEnumerable<T>>(); })
                    .ViaGet()
                    .FetchAsync(token);
                entry.SetSlidingExpiration(TimeSpan.FromSeconds(results != null ? 60 * 60 : 1));
                return results ?? Enumerable.Empty<T>();
            }
        ) ?? Enumerable.Empty<T>();
    }

    private static async Task<IEnumerable<T>> GetHitsAsync<T>(
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
                    .ForJsonResult<JObject>(x =>
                    {
                        if (x.ContainsKey("hits"))
                            results = x["hits"]!.ToObject<IEnumerable<T>>();
                    })
                    .ViaGet()
                    .FetchAsync(token);
                return results ?? Enumerable.Empty<T>();
            }
        ) ?? Enumerable.Empty<T>();
    }

    public static async Task<LabrinthProject?> GetProjectAsync(
        string id,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        var service = $"/project/{id}";
        return await GetResourceAsync<LabrinthProject>(service, cache, token);
    }

    public static async Task<IEnumerable<LabrinthHit>> SearchProjectsAsync(
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
        var facets = new List<KeyValuePair<string, string>>();
        if (gameVersion != null)
            facets.Add(new KeyValuePair<string, string>("versions", gameVersion));

        if (type == ResourceType.Mod && modLoaderId != null)
            facets.Add(
                new KeyValuePair<string, string>(
                    "categories",
                    modLoaderId switch
                    {
                        ComponentMeta.FORGE => "forge",
                        ComponentMeta.FABRIC => "fabric",
                        ComponentMeta.QUILT => "quilt",
                        _ => throw new NotSupportedException()
                    }
                )
            );

        facets.Add(
            new KeyValuePair<string, string>(
                "project_type",
                type switch
                {
                    ResourceType.Modpack => "modpack",
                    ResourceType.ShaderPack => "shader",
                    ResourceType.Mod => "mod",
                    ResourceType.ResourcePack => "resourcepack",
                    _ => throw new NotImplementedException()
                }
            )
        );
        var service =
            $"/search?query={HttpUtility.UrlEncode(query)}&offset={offset}&limit={limit}&facets=[{string.Join(',', facets.Select(x => $"[\"{x.Key}:{x.Value}\"]"))}]";
        return await GetHitsAsync<LabrinthHit>(service, cache, token);
    }

    public static async Task<IEnumerable<LabrinthVersion>> GetProjectVersionsAsync(
        string projectId,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        var service = $"/project/{projectId}/version";
        return await GetResourcesAsync<LabrinthVersion>(service, cache, token);
    }

    public static async Task<LabrinthVersion?> GetVersionAsync(
        string versionId,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        var service = $"/version/{versionId}";
        return await GetResourceAsync<LabrinthVersion>(service, cache, token);
    }

    public static async Task<IEnumerable<LabrinthTeamMember>> GetTeamMembersAsync(
        string teamId,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        var service = $"/team/{teamId}/members";
        return await GetResourcesAsync<LabrinthTeamMember>(service, cache, token);
    }

    public static async Task<(
        IEnumerable<(LabrinthProject, LabrinthVersion)>,
        IEnumerable<string>
        )?> ScanDependenciesAsync(
        LabrinthVersion origin,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        if (token.IsCancellationRequested)
            return null;
        var dep = new List<(LabrinthProject, LabrinthVersion)>();
        var emb = new List<string>();
        if (await ScanDependenciesAsyncInternal(dep, emb, origin, cache, token))
            return (dep, emb);
        return null;
    }

    private static async Task<bool> ScanDependenciesAsyncInternal(
        List<(LabrinthProject, LabrinthVersion)> dep,
        List<string> emb,
        LabrinthVersion origin,
        IMemoryCache cache,
        CancellationToken token = default
    )
    {
        if (!origin.Dependencies.Any())
            return true;
        var filter = (string? p, string? v) => !string.IsNullOrEmpty(v) || !string.IsNullOrEmpty(p);
        var tasks = new List<Task<(LabrinthProject, LabrinthVersion)?>>();
        foreach (var dependency in origin.Dependencies.Where(x => filter(x.ProjectId, x.VersionId)))
            tasks.Add(
                GetVersionPairAsync(dependency.ProjectId, dependency.VersionId, cache, token)
            );
        await Task.WhenAll(tasks);
        if (tasks.All(x => x.IsCompletedSuccessfully && x.Result.HasValue))
        {
            foreach (
                var embedded in origin.Dependencies.Where(
                    x => !filter(x.ProjectId, x.VersionId) && x.FileName != null
                )
            )
                emb.Add(embedded.FileName!);
            var result = true;
            foreach (var task in tasks)
            {
                var tuple = (task.Result!.Value.Item1, task.Result.Value.Item2);
                if (
                    dep.All(
                        x => (x.Item1.Id ?? x.Item1.Slug) != (tuple.Item1.Id ?? tuple.Item1.Slug)
                    )
                )
                {
                    dep.Add(tuple);
                    result &= await ScanDependenciesAsyncInternal(
                        dep,
                        emb,
                        tuple.Item2,
                        cache,
                        token
                    );
                }
            }

            return result;
        }

        return false;
    }

    private static async Task<(LabrinthProject, LabrinthVersion)?> GetVersionPairAsync(
        string? projectId,
        string? versionId,
        IMemoryCache cache,
        CancellationToken token
    )
    {
        if (versionId != null)
        {
            var version = await GetVersionAsync(versionId, cache, token);
            if (version.HasValue)
            {
                var project = await GetProjectAsync(version.Value.ProjectId, cache, token);
                if (project.HasValue) return (project.Value, version.Value);
            }

            return null;
        }

        if (projectId != null)
        {
            var project = await GetProjectAsync(projectId, cache, token);
            if (project.HasValue)
            {
                var version = await GetVersionAsync(project.Value.Versions.Last(), cache, token);
                if (version.HasValue) return (project.Value, version.Value);
            }

            return null;
        }

        return null;
    }
}