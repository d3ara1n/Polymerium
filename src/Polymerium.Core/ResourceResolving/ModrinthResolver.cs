using System;
using System.Linq;
using System.Threading.Tasks;
using DotNext;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;
using Polymerium.Core.Models.Modrinth.Labrinth;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("modrinth")]
public class ModrinthResolver : ResourceResolverBase
{
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
        var modOption = await ModrinthHelper.GetProjectAsync(projectId);
        var versionOption = await ModrinthHelper.GetVersionAsync(version);
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
                project.Description,
                version, new Uri($"poly-res://modrinth@file/mods/{version}")));
    }

    [ResourceType(ResourceType.Mod)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModAsync(string projectId, string version)
    {
        return await GetProjectAsync(ResourceType.Mod, projectId, version,
            (project, _) => new Mod(project.Id ?? project.Slug, project.Title, project.Team, project.IconUrl,
                project.Description,
                version, new Uri($"poly-res://modrinth@file/mods/{version}")));
    }

    [ResourceType(ResourceType.ResourcePack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetResourcePackAsync(string projectId, string version)
    {
        return await GetProjectAsync(ResourceType.Mod, projectId, version,
            (project, _) => new ResourcePack(project.Id ?? project.Slug, project.Title, project.Team, project.IconUrl,
                project.Description,
                version, new Uri($"poly-res://modrinth@file/resourcepacks/{version}")));
    }

    [ResourceType(ResourceType.ShaderPack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetShaderPackAsync(string projectId, string version)
    {
        return await GetProjectAsync(ResourceType.Mod, projectId, version,
            (project, _) => new ResourcePack(project.Id ?? project.Slug, project.Title, project.Team, project.IconUrl,
                project.Description,
                version, new Uri($"poly-res://modrinth@file/shaderpacks/{version}")));
    }

    [ResourceType(ResourceType.File)]
    [ResourceExpression("{dir}/{version}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetFileAsync(string dir, string version)
    {
        var versionOption = await ModrinthHelper.GetVersionAsync(version);
        if (versionOption.TryUnwrap(out var file))
        {
            var first = file.Files.First();
            return Ok(
                new File(file.Id, file.Name, string.Empty, null, string.Empty, version, $"{dir}/{first.Filename}",
                    first.Hashes.Sha1, first.Url), ResourceType.File);
        }

        return Err(ResolveResultError.NotFound);
    }

    [ResourceType(ResourceType.File)]
    [ResourceExpression("{version}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModpackFileAsync(string version)
    {
        var versionOption = await ModrinthHelper.GetVersionAsync(version);
        if (versionOption.TryUnwrap(out var file))
        {
            var first = file.Files.First();
            return Ok(
                new File(file.Id, file.Name, string.Empty, null, string.Empty, version, first.Filename,
                    first.Hashes.Sha1, first.Url), ResourceType.File);
        }

        return Err(ResolveResultError.NotFound);
    }
}