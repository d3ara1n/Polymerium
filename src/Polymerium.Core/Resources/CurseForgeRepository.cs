using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polymerium.Abstractions.Resources;

namespace Polymerium.Core.Resources;

public class CurseForgeRepository : IResourceRepository
{
    public string Label => "CurseForge";
    public ResourceType SupportedResources => ResourceType.Modpack;

    public Task<IEnumerable<Modpack>> SearchModpacksAsync(string query, string? version, uint offset = 0,
        uint limit = 10,
        CancellationToken token = default)
    {
        return Task.FromResult(Enumerable.Empty<Modpack>());
    }
}