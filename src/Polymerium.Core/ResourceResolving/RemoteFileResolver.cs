using System;
using System.IO;
using DotNext;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;
using Polymerium.Abstractions.Resources;
using File = Polymerium.Abstractions.Resources.File;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("remote")]
[ResourceType(ResourceType.File)]
public class RemoteFileResolver : ResourceResolverBase
{
    [ResourceExpression("{*path}")]
    public Result<ResolveResult, ResolveResultError> GetFile(
        string path,
        string source,
        string? sha1 = null
    )
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var fileName = Path.GetFileName(path);
        return Ok(
            new File(name, name, name, fileName, path, sha1, new Uri(source)),
            ResourceType.File
        );
    }
}
