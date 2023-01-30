using System.Collections.Generic;
using Polymerium.Abstractions.ResourceResolving;

namespace Polymerium.Core.Engines;

public class ResolveEngine
{
    private readonly IEnumerable<ResourceResolverBase> _resolvers;

    public ResolveEngine(IEnumerable<ResourceResolverBase> resolvers)
    {
        _resolvers = resolvers;
    }
}