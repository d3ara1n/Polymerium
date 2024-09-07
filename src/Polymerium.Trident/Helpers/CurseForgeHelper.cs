using Microsoft.Extensions.Logging;
using Polymerium.Trident.Models.Eternal;
using Polymerium.Trident.Repositories;
using System.Net.Http.Json;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Helpers;

public static class CurseForgeHelper
{
    private const string API_KEY = "$2a$10$cjd5uExXA6oMi3lSnylNC.xsFJiujI8uQ/pV1eGltFe/hlDO2mjzm";
    private const string ENDPOINT = "https://api.curseforge.com/v1";
    private const string PROJECT_URL = "https://www.curseforge.com/minecraft/{0}/{1}";

    private const uint GAME_ID = 432;
    private const uint CLASSID_MODPACK = 4471;
    private const uint CLASSID_MOD = 6;
    private const uint CLASSID_WORLD = 17;
    private const uint CLASSID_DATAPACK = 4546;
    private const uint CLASSID_SHADERPACK = 6552;
    private const uint CLASSID_RESOURCEPACK = 12;

    //private static readonly Converter MARKDOWNER = new(new Config
    //{
    //    GithubFlavored = true, SmartHrefHandling = true, RemoveComments = true
    //});

    public static readonly IReadOnlyDictionary<string, string> MODLOADER_MAPPINGS = new Dictionary<string, string>
    {
        { "Forge", Loader.COMPONENT_FORGE },
        { "NeoForge", Loader.COMPONENT_NEOFORGE },
        { "Fabric", Loader.COMPONENT_FABRIC },
        { "Quilt", Loader.COMPONENT_QUILT }
    }.AsReadOnly();

    public static string GetUrlTypeStringFromKind(ResourceKind kind) =>
        kind switch
        {
            ResourceKind.Modpack => "modpacks",
            ResourceKind.Mod => "mc-mods",
            ResourceKind.World => "worlds",
            ResourceKind.ResourcePack => "texture-packs",
            ResourceKind.ShaderPack => "shaders",
            ResourceKind.DataPack => "data-packs",
            _ => "unknown"
        };

    public static ResourceKind GetResourceKindFromClassId(uint classId) =>
        classId switch
        {
            CLASSID_MOD => ResourceKind.Mod,
            CLASSID_RESOURCEPACK => ResourceKind.ResourcePack,
            CLASSID_WORLD => ResourceKind.World,
            CLASSID_SHADERPACK => ResourceKind.ShaderPack,
            CLASSID_MODPACK => ResourceKind.Modpack,
            CLASSID_DATAPACK => ResourceKind.DataPack,
            _ => throw new NotSupportedException()
        };

    public static uint GetClassIdFromResourceKind(ResourceKind kind) =>
        kind switch
        {
            ResourceKind.Modpack => CLASSID_MODPACK,
            ResourceKind.Mod => CLASSID_MOD,
            ResourceKind.ResourcePack => CLASSID_RESOURCEPACK,
            ResourceKind.ShaderPack => CLASSID_SHADERPACK,
            ResourceKind.World => CLASSID_WORLD,
            ResourceKind.DataPack => CLASSID_DATAPACK,
            _ => throw new NotSupportedException()
        };

    private static async Task<T> GetResourceAsync<T>(ILogger logger, IHttpClientFactory factory,
        string service, CancellationToken token = default)
        where T : struct
    {
        token.ThrowIfCancellationRequested();
        var url = ENDPOINT + service;
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", API_KEY);
        try
        {
            var response = await client.GetFromJsonAsync<ResponseWrapper<T>>(url, token);
            return response.Data;
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed to get {} from CurseForge for {}", service, e.Message);
            throw;
        }
    }

    private static async Task<string> GetStringAsync(ILogger logger, IHttpClientFactory factory, string service,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var url = ENDPOINT + service;
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", API_KEY);
        try
        {
            var response = await client.GetFromJsonAsync<ResponseWrapper<string>>(url, token);
            return response.Data;
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed to get {} from CurseForge for {}", service, e.Message);
            throw;
        }
    }

    private static async Task<IEnumerable<T>> GetResourcesAsync<T>(ILogger logger, IHttpClientFactory factory,
        string service, CancellationToken token = default)
        where T : struct
    {
        token.ThrowIfCancellationRequested();
        var url = ENDPOINT + service;
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", API_KEY);
        try
        {
            var response = await client.GetFromJsonAsync<ResponseWrapper<IEnumerable<T>>>(url, token);
            return response.Data;
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed to get {} from CurseForge for {}", service, e.Message);
            throw;
        }
    }

    public static async Task<IEnumerable<EternalMod>> SearchProjectsAsync(ILogger logger,
        IHttpClientFactory factory, string query, ResourceKind kind, string? gameVersion = null,
        string? modLoaderId = null, uint offset = 0, uint limit = 10, CancellationToken token = default)
    {
        var modLoaderType = modLoaderId switch
        {
            Loader.COMPONENT_FORGE => 1,
            Loader.COMPONENT_NEOFORGE => 6,
            Loader.COMPONENT_FABRIC => 4,
            Loader.COMPONENT_QUILT => 5,
            _ => 0
        };
        var service =
            $"/mods/search?gameId={GAME_ID}&classId={GetClassIdFromResourceKind(kind)}&index={offset}&pageSize={limit}&searchFilter={Uri.EscapeDataString(query)}&sortOrder=desc"
            + (gameVersion != null ? $"&gameVersion={gameVersion}" : "")
            + (
                (kind == ResourceKind.Mod || kind == ResourceKind.Modpack) && modLoaderId != null
                    ? $"&modLoaderType={modLoaderType}"
                    : ""
            );
        return await GetResourcesAsync<EternalMod>(logger, factory, service, token);
    }

    public static async Task<EternalMod> GetModInfoAsync(ILogger logger, IHttpClientFactory factory, uint projectId,
        CancellationToken token = default)
    {
        var service = $"/mods/{projectId}";
        return await GetResourceAsync<EternalMod>(logger, factory, service, token);
    }

    public static async Task<string> GetModDescriptionAsync(ILogger logger, IHttpClientFactory factory,
        uint projectId,
        CancellationToken token = default)
    {
        var service = $"/mods/{projectId}/description";
        return await GetStringAsync(logger, factory, service, token);
    }

    public static async Task<string> GetModDownloadUrlAsync(ILogger logger, IHttpClientFactory factory,
        uint projectId,
        uint fileId, CancellationToken token = default)
    {
        var service = $"/mods/{projectId}/files/{fileId}/download-url";
        return await GetStringAsync(logger, factory, service, token);
    }

    public static async Task<string> GetModFileChangelogAsync(ILogger logger, IHttpClientFactory factory,
        uint projectId, uint fileId, CancellationToken token = default)
    {
        var service = $"/mods/{projectId}/files/{fileId}/changelog";
        return await GetStringAsync(logger, factory, service, token);
    }

    public static async Task<IEnumerable<EternalModInfo>> GetModFilesAsync(ILogger logger,
        IHttpClientFactory factory,
        uint projectId, string? gameVersion, string? modLoader, CancellationToken token = default)
    {
        var queries = new List<string>();
        if (gameVersion != null)
        {
            queries.Add("gameVersion=" + gameVersion);
        }

        if (modLoader != null && MODLOADER_MAPPINGS.Values.Contains(modLoader))
        {
            queries.Add("modLoaderType=" + MODLOADER_MAPPINGS.First(x => x.Value == modLoader).Key);
        }

        var services = queries.Count > 0
            ? $"/mods/{projectId}/files?{string.Join("&", queries)}"
            : $"/mods/{projectId}/files";
        return await GetResourcesAsync<EternalModInfo>(logger, factory, services, token);
    }

    public static async Task<EternalModInfo> GetModFileInfoAsync(ILogger logger, IHttpClientFactory factory,
        uint projectId, uint versionId, CancellationToken token = default)
    {
        var service = $"/mods/{projectId}/files/{versionId}";
        return await GetResourceAsync<EternalModInfo>(logger, factory, service, token);
    }

    public static async Task<Project> GetIntoProjectAsync(ILogger logger, IHttpClientFactory factory,
        uint projectId, CancellationToken token = default)
    {
        var mod = await GetModInfoAsync(logger, factory, projectId, token);
        var modDesc = await GetModDescriptionAsync(logger, factory, projectId, token);
        var files = (await GetModFilesAsync(logger, factory, projectId, null, null, token)).ToArray();
        var versionTasks = files
            .Where(x => x is { IsAvailable: true, IsServerPack: false, FileStatus: 4 })
            .OrderByDescending(x => x.FileDate).Select(async x =>
            {
                var changelog = await GetModFileChangelogAsync(logger, factory, projectId, x.Id, token);
                return new Project.Version(x.Id.ToString(), x.DisplayName, changelog,
                    x.ExtractReleaseType(),
                    x.FileDate,
                    x.FileName, x.ExtractSha1(), x.ExtractDownloadUrl(),
                    x.ExtractRequirement(), x.ExtractDependencies());
            }).ToList();
        await Task.WhenAll(versionTasks);
        var versions = versionTasks.Where(x => x.IsCompletedSuccessfully)
            .Select(x => x.Result).ToList();
        var kind = GetResourceKindFromClassId(mod.ClassId);
        return new Project(
            mod.Id.ToString(),
            mod.Name,
            RepositoryLabels.CURSEFORGE,
            mod.Logo?.ThumbnailUrl,
            string.Join(", ", mod.Authors.Select(x => x.Name)),
            mod.Summary,
            new Uri(PROJECT_URL.Replace("{0}", GetUrlTypeStringFromKind(kind)).Replace("{1}", mod.Slug)),
            kind,
            mod.DateCreated,
            mod.DateModified,
            mod.DownloadCount, modDesc,
            mod.Screenshots.Select(x => new Project.Screenshot(x.Title, x.Url)).ToList(),
            versions);
    }

    public static async Task<Package> GetIntoPackageAsync(ILogger logger, IHttpClientFactory factory,
        uint projectId, uint? versionId, string? gameVersion, string? modLoader, CancellationToken token = default)
    {
        var mod = await GetModInfoAsync(logger, factory, projectId, token);
        var kind = GetResourceKindFromClassId(mod.ClassId);
        EternalModInfo? file = null;
        if (versionId.HasValue)
        {
            file = await GetModFileInfoAsync(logger, factory, projectId, versionId.Value, token);
        }
        else
        {
            var files = (await GetModFilesAsync(logger, factory, projectId, gameVersion, modLoader, token)).ToArray();

            file = files.MaxBy(x => x.FileDate);
        }

        if (file.HasValue)
        {
            var package = new Package(
                mod.Id.ToString(),
                mod.Name,
                file.Value.Id.ToString(),
                file.Value.DisplayName,
                RepositoryLabels.CURSEFORGE,
                mod.Logo?.Url,
                string.Join(", ", mod.Authors.Select(x => x.Name)),
                mod.Summary,
                new Uri(PROJECT_URL.Replace("{0}", GetUrlTypeStringFromKind(kind))
                    .Replace("{1}", mod.Slug)),
                kind,
                file.Value.ExtractReleaseType(),
                file.Value.FileDate,
                file.Value.FileName,
                file.Value.ExtractDownloadUrl(),
                file.Value.ExtractSha1(),
                file.Value.ExtractRequirement(),
                file.Value.ExtractDependencies()
            );
            return package;
        }

        throw new ResourceNotFoundException(
            $"({RepositoryLabels.CURSEFORGE},{projectId},{(versionId.HasValue ? versionId.ToString() : "any")},Filter({gameVersion},{modLoader}))");
    }

    public struct ResponseWrapper<T>
    {
        public T Data { get; init; }
    }
}