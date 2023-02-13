using System;
using Polymerium.Abstractions;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("local")]
[ResourceType("file")]
public class LocalFileResolver : ResourceResolverBase
{
    [ResourceExpression("{*path}")]
    public Result<ResolveResult, ResolveResultError> GetFile(string path)
    {
        return Ok(
            new Uri(new Uri($"poly-file://{Context.Instance.Id}/"), path),
            new Uri(new Uri($"poly-file:///local/instances/{Context.Instance.Id}/"), path),
            null
        );
    }
}
