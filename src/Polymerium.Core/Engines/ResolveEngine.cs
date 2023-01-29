using Polymerium.Abstractions.ResourceResolving;
using System.Collections.Generic;

namespace Polymerium.Core.Engines
{
    public class ResolveEngine
    {
        private readonly IEnumerable<ResourceResolverBase> _resolvers;

        public ResolveEngine(IEnumerable<ResourceResolverBase> resolvers)
        {
            _resolvers = resolvers;
        }
    }
}