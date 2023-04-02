using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;

namespace Polymerium.Core.Resources;

public class ModrinthRepository : IResourceRepository
{
    private readonly IMemoryCache _cache;

    public ModrinthRepository(IMemoryCache cache)
    {
        _cache = cache;
    }

    public RepositoryLabel Label => RepositoryLabel.Modrinth;

    public ResourceType SupportedResources =>
        ResourceType.Modpack | ResourceType.Mod | ResourceType.ShaderPack | ResourceType.ResourcePack;

    public async Task<IEnumerable<RepositoryAssetMeta>> SearchProjectsAsync(string query, ResourceType type,
        string? modLoader, string? version,
        uint offset = 0, uint limit = 10, CancellationToken token = default)
    {
        var results =
            await ModrinthHelper.SearchProjectsAsync(_cache, query, type, version, modLoader, offset, limit, token);
        return results.Select(x =>
            new RepositoryAssetMeta
            {
                Repository = RepositoryLabel.Modrinth,
                Id = x.ProjectId,
                Name = x.Title,
                Author = x.Author,
                IconSource = x.IconUrl,
                Summary = x.Description,
                Type = type,
                Versions = x.Versions
            });
    }

    public async Task<RepositoryAssetMeta?> GetModAsync(string id, CancellationToken token = default)
    {
        var option = await ModrinthHelper.GetProjectAsync(id, _cache, token);
        if (option.TryUnwrap(out var project))
        {
            var result = new RepositoryAssetMeta
            {
                Repository = RepositoryLabel.Modrinth,
                Id = id,
                Name = project.Title,
                Author = project.Team,
                IconSource = project.IconUrl,
                Summary = project.Description,
                Type = ResourceType.Mod,
                Versions = project.Versions
            };
            return result;
        }

        return null;
    }
}