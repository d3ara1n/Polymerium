using System;
using System.Linq;
using System.Threading.Tasks;
using DotNext;
using Microsoft.Extensions.Caching.Memory;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;
using Polymerium.Core.Models.CurseForge.Eternal;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("curseforge")]
public class CurseForgeResolver : ResourceResolverBase
{
    private const string CURSEFORGE_PROJECT_URL = "https://beta.curseforge.com/minecraft/{0}/{1}";
    private static readonly Func<string, uint> PARSER_INT = uint.Parse;

    private readonly IMemoryCache _cache;

    public CurseForgeResolver(IMemoryCache cache)
    {
        _cache = cache;
    }

    public static Uri MakeResourceUrl(ResourceType type, string projectId, string version)
    {
        return type switch
        {
            ResourceType.Update => new Uri($"poly-res://curseforge@update/{projectId}"),
            ResourceType.File => new Uri($"poly-res://curseforge@file/{projectId}/{version}"),
            _
                => new Uri(
                    $"poly-res://curseforge@{type.ToString().ToLower()}/{projectId}?version={version}"
                )
        };
    }

    private async Task<Result<ResolveResult, ResolveResultError>> GetProjectAsync(
        ResourceType type,
        string projectId,
        string version,
        Func<EternalProject, EternalModFile, ResourceBase> cast
    )
    {
        var pid = PARSER_INT.TryInvoke(projectId);
        // version 其实就是 fileId
        var fid = PARSER_INT.TryInvoke(version);
        if (pid.IsSuccessful && fid.IsSuccessful)
        {
            var mod = await CurseForgeHelper.GetModInfoAsync(pid.Value, _cache);
            var file = await CurseForgeHelper.GetModFileInfoAsync(pid.Value, fid.Value, _cache);
            if (mod.HasValue && file.HasValue)
                return new ResolveResult(cast(mod.Value, file.Value), type);

            return Err(ResolveResultError.NotFound);
        }

        return Err(ResolveResultError.InvalidArguments);
    }

    [ResourceType(ResourceType.Modpack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModpackAsync(
        string projectId,
        string version
    )
    {
        return await GetProjectAsync(
            ResourceType.Modpack,
            projectId,
            version,
            (project, file) =>
                new Modpack(
                    project.Id.ToString(),
                    project.Name,
                    file.DisplayName,
                    string.Join(", ", project.Authors.Select(x => x.Name)),
                    project.Logo?.ThumbnailUrl,
                    new Uri(
                        CURSEFORGE_PROJECT_URL
                            .Replace("{0}", "modpacks")
                            .Replace("{1}", project.Slug)
                    ),
                    project.Summary,
                    version,
                    MakeResourceUrl(ResourceType.Update, projectId, version),
                    MakeResourceUrl(ResourceType.File, projectId, version)
                )
        );
    }

    [ResourceType(ResourceType.Mod)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModAsync(
        string projectId,
        string version
    )
    {
        return await GetProjectAsync(
            ResourceType.Mod,
            projectId,
            version,
            (project, file) =>
                new Mod(
                    project.Id.ToString(),
                    project.Name,
                    file.DisplayName,
                    string.Join(", ", project.Authors.Select(x => x.Name)),
                    project.Logo?.ThumbnailUrl,
                    new Uri(
                        CURSEFORGE_PROJECT_URL
                            .Replace("{0}", "mc-mods")
                            .Replace("{1}", project.Slug)
                    ),
                    project.Summary,
                    version,
                    MakeResourceUrl(ResourceType.Update, projectId, version),
                    MakeResourceUrl(ResourceType.File, projectId, version)
                )
        );
    }

    [ResourceType(ResourceType.ResourcePack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetResourcePackAsync(
        string projectId,
        string version
    )
    {
        return await GetProjectAsync(
            ResourceType.ResourcePack,
            projectId,
            version,
            (project, file) =>
                new ResourcePack(
                    project.Id.ToString(),
                    project.Name,
                    file.DisplayName,
                    string.Join(", ", project.Authors.Select(x => x.Name)),
                    project.Logo?.ThumbnailUrl,
                    new Uri(
                        CURSEFORGE_PROJECT_URL
                            .Replace("{0}", "texture-packs")
                            .Replace("{1}", project.Slug)
                    ),
                    project.Summary,
                    version,
                    MakeResourceUrl(ResourceType.Update, projectId, version),
                    MakeResourceUrl(ResourceType.File, projectId, version)
                )
        );
    }

    [ResourceType(ResourceType.File)]
    [ResourceExpression("{projectId}/{fileId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetFileAsync(
        string projectId,
        string fileId
    )
    {
        var pid = PARSER_INT.TryInvoke(projectId);
        // version 其实就是 fileId
        var fid = PARSER_INT.TryInvoke(fileId);
        if (pid.IsSuccessful && fid.IsSuccessful)
        {
            var mod = await CurseForgeHelper.GetModInfoAsync(pid.Value, _cache);
            var file = await CurseForgeHelper.GetModFileInfoAsync(pid.Value, fid.Value, _cache);
            if (mod.HasValue && file.HasValue)
            {
                var sha1 = file.Value.ExtractSha1();
                var fileName = mod.Value.ClassId switch
                {
                    6 => $"mods/{file.Value.FileName}",
                    12 => $"resourcepacks/{file.Value.FileName}",
                    17 => $"worlds/{file.Value.FileName}",
                    4546 => $"shaderpacks/{file.Value.FileName}",
                    4471 => file.Value.FileName,
                    _ => throw new NotImplementedException()
                };
                var result = new File(
                    mod.Value.Id.ToString(),
                    file.Value.DisplayName,
                    file.Value.DisplayName,
                    file.Value.Id.ToString(),
                    fileName,
                    sha1,
                    file.Value.ExtractDownloadUrl()
                );
                return Ok(result, ResourceType.File);
            }

            return Err(ResolveResultError.NotFound);
        }

        return Err(ResolveResultError.InvalidArguments);
    }
}
