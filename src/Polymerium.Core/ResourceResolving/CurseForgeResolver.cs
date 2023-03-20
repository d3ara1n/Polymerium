using System;
using System.Linq;
using System.Threading.Tasks;
using DotNext;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;
using Polymerium.Core.Models.CurseForge.Eternal;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("curseforge")]
public class CurseForgeResolver : ResourceResolverBase
{
    private static readonly Func<string, uint> PARSER_INT = uint.Parse;
    private const string CURSEFORGE_PROJECT_URL = "https://beta.curseforge.com/minecraft/{0}/{1}";

    public static Uri MakeResourceUrl(ResourceType type, string projectId, string version)
    {
        return type switch
        {
            ResourceType.File => new Uri($"poly-res://curseforge@file/{projectId}/{version}"),
            _ => new Uri($"poly-res://curseforge@{type.ToString().ToLower()}/{projectId}?version={version}")
        };
    }

    private async Task<Result<ResolveResult, ResolveResultError>> GetProjectAsync(ResourceType type, string projectId,
        string version, Func<EternalProject, EternalModFile, ResourceBase> cast)
    {
        var pid = PARSER_INT.TryInvoke(projectId);
        // version 其实就是 fileId
        var fid = PARSER_INT.TryInvoke(version);
        if (pid.IsSuccessful && fid.IsSuccessful)
        {
            var modOption = await CurseForgeHelper.GetModInfoAsync(pid.Value);
            var fileOption = await CurseForgeHelper.GetModFileInfoAsync(pid.Value, fid.Value);
            if (modOption.TryUnwrap(out var eternalProject) && fileOption.TryUnwrap(out var eternalFile))
                return new ResolveResult(cast(eternalProject, eternalFile), type);

            return Err(ResolveResultError.NotFound);
        }

        return Err(ResolveResultError.InvalidArguments);
    }

    [ResourceType(ResourceType.Modpack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModpackAsync(string projectId, string version)
    {
        return await GetProjectAsync(ResourceType.Modpack, projectId, version,
            (project, _) => new Modpack(project.Id.ToString(), project.Name,
                string.Join(", ", project.Authors.Select(x => x.Name)), project.Logo?.ThumbnailUrl,
                new Uri(CURSEFORGE_PROJECT_URL.Replace("{0}", "modpacks").Replace("{1}", project.Slug)),
                project.Summary,
                version, new Uri($"poly-res://curseforge@file/{projectId}/{version}")));
    }

    [ResourceType(ResourceType.Mod)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetModAsync(string projectId, string version)
    {
        return await GetProjectAsync(ResourceType.Mod, projectId, version,
            (project, _) => new Mod(project.Id.ToString(), project.Name,
                string.Join(", ", project.Authors.Select(x => x.Name)), project.Logo?.ThumbnailUrl,
                new Uri(CURSEFORGE_PROJECT_URL.Replace("{0}", "mc-mods").Replace("{1}", project.Slug)), project.Summary,
                version, new Uri($"poly-res://curseforge@file/{projectId}/{version}")));
    }

    [ResourceType(ResourceType.ResourcePack)]
    [ResourceExpression("{projectId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetResourcePackAsync(string projectId, string version)
    {
        return await GetProjectAsync(ResourceType.Mod, projectId, version,
            (project, _) => new ResourcePack(project.Id.ToString(), project.Name,
                string.Join(", ", project.Authors.Select(x => x.Name)), project.Logo?.ThumbnailUrl,
                new Uri(CURSEFORGE_PROJECT_URL.Replace("{0}", "texture-packs").Replace("{1}", project.Slug)),
                project.Summary,
                version, new Uri($"poly-res://curseforge@file/{projectId}/{version}")));
    }

    [ResourceType(ResourceType.File)]
    [ResourceExpression("{projectId}/{fileId}")]
    public async Task<Result<ResolveResult, ResolveResultError>> GetFileAsync(string projectId, string fileId)
    {
        var pid = PARSER_INT.TryInvoke(projectId);
        // version 其实就是 fileId
        var fid = PARSER_INT.TryInvoke(fileId);
        if (pid.IsSuccessful && fid.IsSuccessful)
        {
            var modOption = await CurseForgeHelper.GetModInfoAsync(pid.Value);
            var fileOption = await CurseForgeHelper.GetModFileInfoAsync(pid.Value, fid.Value);
            if (modOption.TryUnwrap(out var eternalProject) && fileOption.TryUnwrap(out var eternalFile))
            {
                var sha1 = eternalFile.ExtractSha1();
                var fileName = eternalProject.ClassId switch
                {
                    6 => $"mods/{eternalFile.FileName}",
                    12 => $"resourcepacks/{eternalFile.FileName}",
                    17 => $"worlds/{eternalFile.FileName}",
                    4546 => $"shaderpacks/{eternalFile.FileName}",
                    4471 => eternalFile.FileName,
                    _ => throw new NotImplementedException()
                };
                var file = new File(eternalFile.Id.ToString(), eternalFile.DisplayName, string.Empty, null, null,
                    string.Empty, fileId, fileName, sha1,
                    eternalFile.ExtractDownloadUrl());
                return Ok(file, ResourceType.File);
            }
            return Err(ResolveResultError.NotFound);
        }
        return Err(ResolveResultError.InvalidArguments);
    }
}