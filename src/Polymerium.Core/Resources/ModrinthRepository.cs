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
    public ResourceType SupportedResources => ResourceType.Modpack;

    public async Task<IEnumerable<RepositoryAssetMeta>> SearchModpacksAsync(string query, string? version,
        uint offset = 0,
        uint limit = 10, CancellationToken token = default)
    {
        var results =
            await ModrinthHelper.SearchProjectsAsync(query, ResourceType.Modpack, version, null, offset, limit, token);
        return results.Select(x =>
            new RepositoryAssetMeta
            {
                Id = x.ProjectId,
                Name = x.Title,
                Author = x.Author,
                IconSource = x.IconUrl,
                Summary = x.Description,
                Type = ResourceType.Modpack
            });
    }
}