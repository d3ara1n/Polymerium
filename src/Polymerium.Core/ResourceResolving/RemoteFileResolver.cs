using System;
using Polymerium.Abstractions;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("remote")]
[ResourceType("file")]
public class RemoteFileResolver : ResourceResolverBase
{
    [ResourceExpression("{path}")]
    public Result<ResolveResult, ResolveResultError> GetFile(Uri path, string sha1, Uri source)
    {
        return Ok(path, source, sha1);
    }
}