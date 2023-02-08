using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Polymerium.Abstractions;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;

namespace Polymerium.Core.Engines;

public class ResolveEngine
{
    private readonly IEnumerable<ResourceResolverBase> _resolvers;
    private readonly IEnumerable<ResolverTuple> _tuples;

    public ResolveEngine(IEnumerable<ResourceResolverBase> resolvers)
    {
        _resolvers = resolvers;
        _tuples = _resolvers.SelectMany(GetTuplesInType);
    }

    private IEnumerable<ResolverTuple> GetTuplesInType(ResourceResolverBase resolver)
    {
        var res = resolver.GetType();
        var domain = res.GetCustomAttribute<ResourceDomainAttribute>();
        var domainName = domain?.DomainName;
        var type = res.GetCustomAttribute<ResourceTypeAttribute>();
        var methods = res.GetMethods();
        foreach (var method in methods)
            if (method.IsPublic)
            {
                var methodType = method.GetCustomAttribute<ResourceTypeAttribute>();
                var realType = methodType ?? type;
                if (realType != null) yield return new ResolverTuple(realType.TypeName, domainName, method, resolver);
            }
    }

    public Result<ResolveResult, ResolveResultError> Resolve(Uri resource)
    {
        throw new NotImplementedException();
    }

    private record ResolverTuple(string TypeName, string? DomainName, MethodInfo Method, ResourceResolverBase Self);
}