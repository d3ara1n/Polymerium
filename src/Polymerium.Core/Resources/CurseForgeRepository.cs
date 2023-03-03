using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNext.Threading;
using Polymerium.Abstractions.Resources;
using Polymerium.Core.Helpers;

namespace Polymerium.Core.Resources;

public class CurseForgeRepository : IResourceRepository
{
    public string Label => "CurseForge";
    public ResourceType SupportedResources => ResourceType.Modpack;


    public async Task<IEnumerable<Modpack>> SearchModpacksAsync(string query, string? version, uint offset = 0,
        uint limit = 10,
        CancellationToken token = default)
    {
        var results =
            await CurseForgeHelper.SearchProjectsAsync(query, ResourceType.Modpack, version, null, offset, limit,
                token);
        return results.Select(x => new Modpack(x.Id.ToString(), x.Name,
            string.Join(", ", x.Authors.Select(y => y.Name)), x.Logo.ThumbnailUrl, x.Summary, new AsyncLazy<string>(
                async () =>
                {
                    var result = await CurseForgeHelper.GetModDescriptionAsync(x.Id);
                    if (result.TryUnwrap(out var body))
                        return body;
                    return x.Summary;
                })));
    }
}