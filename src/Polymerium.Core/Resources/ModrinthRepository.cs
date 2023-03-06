using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;

namespace Polymerium.Core.Resources;

public class ModrinthRepository : IResourceRepository
{
    public RepositoryLabel Label => RepositoryLabel.Modrinth;

    public ResourceType SupportedResources =>
        ResourceType.Modpack | ResourceType.Mod | ResourceType.Shader | ResourceType.ResourcePack;

    public async Task<IEnumerable<RepositoryAssetMeta>> SearchProjectsAsync(string query, ResourceType type,
        string? version,
        uint offset = 0,
        uint limit = 10, CancellationToken token = default)
    {
        var results =
            await ModrinthHelper.SearchProjectsAsync(query, type, version, null, offset, limit, token);
        return results.Select(x =>
            new RepositoryAssetMeta
            {
                Id = x.ProjectId,
                Name = x.Title,
                Author = x.Author,
                IconSource = x.IconUrl,
                Summary = x.Description,
                Type = type
            });
    }

    public async Task<RepositoryAssetMeta?> GetModAsync(string id, CancellationToken token = default)
    {
        var option = await ModrinthHelper.GetProjectAsync(id, token);
        if (option.TryUnwrap(out var project))
        {
            var result = new RepositoryAssetMeta
            {
                Id = id,
                Name = project.Title,
                Author = project.Team,
                IconSource = project.IconUrl,
                Summary = project.Description,
                Type = ResourceType.Mod
            };
            return result;
        }

        return null;
    }
}