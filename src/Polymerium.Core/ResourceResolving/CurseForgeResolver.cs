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
#pragma warning disable S1075 // URIs should not be hardcoded
    private const string CURSEFORGE_PROJECT_URL = "https://beta.curseforge.com/minecraft/{0}/{1}";
#pragma warning restore S1075 // URIs should not be hardcoded
    private static readonly Func<string, uint> PARSER_INT = uint.Parse;

    private readonly IMemoryCache _cache;

    public CurseForgeResolver(IMemoryCache cache)
    {
        _cache = cache;
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
                    CurseForgeHelper.MakeResourceUrl(ResourceType.Update, projectId, version),
                    CurseForgeHelper.MakeResourceUrl(ResourceType.File, projectId, version)
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
                    CurseForgeHelper.MakeResourceUrl(ResourceType.Update, projectId, version),
                    CurseForgeHelper.MakeResourceUrl(ResourceType.File, projectId, version)
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
                    CurseForgeHelper.MakeResourceUrl(ResourceType.Update, projectId, version),
                    CurseForgeHelper.MakeResourceUrl(ResourceType.File, projectId, version)
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
                var type = CurseForgeHelper.GetResourceTypeFromClassId(mod.Value.ClassId);
                var fileName = type switch
                {
                    ResourceType.Mod => $"mods/{file.Value.FileName}",
                    ResourceType.ResourcePack => $"resourcepacks/{file.Value.FileName}",
                    ResourceType.World => $"worlds/{file.Value.FileName}",
                    ResourceType.ShaderPack => $"shaderpacks/{file.Value.FileName}",
                    ResourceType.Modpack => file.Value.FileName,
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
