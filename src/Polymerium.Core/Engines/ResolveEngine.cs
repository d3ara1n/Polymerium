using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.Abstractions.ResourceResolving;

namespace Polymerium.Core.Engines
{
    public class ResolveEngine
    {
        private readonly IEnumerable<IResourceResolver> _resolvers;
        public ResolveEngine(IEnumerable<IResourceResolver> resolvers)
        {
            _resolvers = resolvers;
        }
    }
}
