using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;

namespace Polymerium.Core.Resources;

public class CurseForgeRepository : IResourceRepository
{
    public RepositoryLabel Label => RepositoryLabel.CurseForge;
    public ResourceType SupportedResources => ResourceType.Modpack;


    public async Task<IEnumerable<RepositoryAssetMeta>> SearchModpacksAsync(string query, string? version,
        uint offset = 0,
        uint limit = 10,
        CancellationToken token = default)
    {
        var results =
            await CurseForgeHelper.SearchProjectsAsync(query, ResourceType.Modpack, version, null, offset, limit,
                token);
        return results.Select(x => new RepositoryAssetMeta
        {
            Id = x.Id.ToString(),
            Name = x.Name,
            Author = string.Join(", ", x.Authors.Select(y => y.Name)),
            IconSource = x.Logo?.ThumbnailUrl ?? new Uri("ms-appx:///Assets/Placeholders/default_world_icon.png"),
            Summary = x.Summary,
            Type = ResourceType.Modpack
        });
    }
}