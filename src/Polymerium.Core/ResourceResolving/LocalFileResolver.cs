using System;
using System.IO;
using DotNext;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;
using Polymerium.Abstractions.Resources;
using File = Polymerium.Abstractions.Resources.File;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("local")]
[ResourceType(ResourceType.File)]
public class LocalFileResolver : ResourceResolverBase
{
    [ResourceExpression("{*path}")]
    public Result<ResolveResult, ResolveResultError> GetFile(string path)
    {
        var name = Path.GetFileName(path);
        return Context.Instance != null
            ? Ok(
                new File(name, name, string.Empty, string.Empty, null, null, string.Empty, string.Empty, path, null,
                new Uri(new Uri($"poly-file:///local/instances/{Context.Instance!.Id}/"), path)), ResourceType.File)
            : Err(ResolveResultError.NotFound);
    }
}