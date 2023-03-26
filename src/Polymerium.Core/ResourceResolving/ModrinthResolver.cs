using System;
using System.Linq;
using System.Threading.Tasks;
using DotNext;
using Microsoft.Extensions.Caching.Memory;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;
using Polymerium.Core.Models.Modrinth.Labrinth;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("modrinth")]
public class ModrinthResolver : ResourceResolverBase
{
    private const string MODRINTH_PROJECT_URL = "https://modrinth.com/{0}/{1}";

    private readonly IMemoryCache _cache;

    public ModrinthResolver(IMemoryCache cache)
    {
        _cache = cache;
    }

    public static Uri MakeResourceUrl(ResourceType type, string projectId, string version)
    {
        return type switch
        {
            ResourceType.File => throw new NotSupportedException(),
            _ => new Uri($"poly-res://modrinth@{type.ToString().ToLower()}/{projectId}?version={version}")
        };
    }

    private async Task<Result<ResolveResult, ResolveResultError>> GetProjectAsync(ResourceType type, string projectId,
        string version, Func<LabrinthProject, LabrinthVersion, ResourceBase> cast)
    {
        var modOption = await ModrinthHelper.GetProjectAsync(projectId, _cache);
        var versionOption = await ModrinthHelper.GetVersionAsync(version, _cache);
        if (modOption.TryUnwrap(out var project) && versionOption.TryUnwrap(out var file))
            return new ResolveResult(cast(project, file), type);

        return Err(ResolveResultError.NotFound);
    }

    [ResourceType(ResourceType.Modpack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModPackAsync(string projectId, string version)
    {
        return await GetProjectAsync(ResourceType.Mod, projectId, version,
            (project, _) => new Modpack(project.Id ?? project.Slug, project.Title, project.Team, project.IconUrl,
                new Uri(MODRINTH_PROJECT_URL.Replace("{0}", "modpack").Replace("{1}", project.Slug)),
                project.Description,
                version, new Uri($"poly-res://modrinth@file/mods/{version}")));
    }

    [ResourceType(ResourceType.Mod)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModAsync(string projectId, string version)
    {
        return await GetProjectAsync(ResourceType.Mod, projectId, version,
            (project, _) => new Mod(project.Id ?? project.Slug, project.Title, project.Team, project.IconUrl,
                new Uri(MODRINTH_PROJECT_URL.Replace("{0}", "mod").Replace("{1}", project.Slug)),
                project.Description,
                version, new Uri($"poly-res://modrinth@file/mods/{version}")));
    }

    [ResourceType(ResourceType.ResourcePack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetResourcePackAsync(string projectId, string version)
    {
        return await GetProjectAsync(ResourceType.Mod, projectId, version,
            (project, _) => new ResourcePack(project.Id ?? project.Slug, project.Title, project.Team, project.IconUrl,
                new Uri(MODRINTH_PROJECT_URL.Replace("{0}", "resourcepack").Replace("{1}", project.Slug)),
                project.Description,
                version, new Uri($"poly-res://modrinth@file/resourcepacks/{version}")));
    }

    [ResourceType(ResourceType.ShaderPack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetShaderPackAsync(string projectId, string version)
    {
        return await GetProjectAsync(ResourceType.Mod, projectId, version,
            (project, _) => new ResourcePack(project.Id ?? project.Slug, project.Title, project.Team, project.IconUrl,
                new Uri(MODRINTH_PROJECT_URL.Replace("{0}", "shader").Replace("{1}", project.Slug)),
                project.Description,
                version, new Uri($"poly-res://modrinth@file/shaderpacks/{version}")));
    }

    [ResourceType(ResourceType.File)]
    [ResourceExpression("{dir}/{version}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetFileAsync(string dir, string version)
    {
        var versionOption = await ModrinthHelper.GetVersionAsync(version, _cache);
        if (versionOption.TryUnwrap(out var file))
        {
            var first = file.Files.First();
            return Ok(
                new File(file.Id, file.Name, string.Empty, null, null, string.Empty, version, $"{dir}/{first.Filename}",
                    first.Hashes.Sha1, first.Url), ResourceType.File);
        }

        return Err(ResolveResultError.NotFound);
    }

    [ResourceType(ResourceType.File)]
    [ResourceExpression("{version}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModpackFileAsync(string version)
    {
        var versionOption = await ModrinthHelper.GetVersionAsync(version, _cache);
        if (versionOption.TryUnwrap(out var file))
        {
            var first = file.Files.First();
            return Ok(
                new File(file.Id, file.Name, string.Empty, null, null, string.Empty, version, first.Filename,
                    first.Hashes.Sha1, first.Url), ResourceType.File);
        }

        return Err(ResolveResultError.NotFound);
    }
}