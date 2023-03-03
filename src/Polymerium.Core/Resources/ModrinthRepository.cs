using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNext.Threading;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;

namespace Polymerium.Core.Resources;

public class ModrinthRepository : IResourceRepository
{
    public string Label => "Modrinth";
    public ResourceType SupportedResources => ResourceType.Modpack;

    public async Task<IEnumerable<Modpack>> SearchModpacksAsync(string query, string? version, uint offset = 0,
        uint limit = 10, CancellationToken token = default)
    {
        var results =
            await ModrinthHelper.SearchProjectsAsync(query, ResourceType.Modpack, version, null, offset, limit, token);
        return results.Select(x =>
            new Modpack(x.Id, x.Title, x.Team, x.IconUrl, x.Description, new AsyncLazy<string>(x.Body)));
    }
}