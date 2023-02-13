using System;
using Polymerium.Abstractions;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("remote")]
[ResourceType("file")]
public class RemoteFileResolver : ResourceResolverBase
{
    [ResourceExpression("{*path}")]
    public Result<ResolveResult, ResolveResultError> GetFile(
        string path,
        string sha1,
        string source
    )
    {
        return Ok(new Uri(path, UriKind.Relative), new Uri(source, UriKind.Absolute), sha1);
    }
}
