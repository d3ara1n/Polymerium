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
        var fileName = Path.GetFileName(path);
        var name = Path.GetFileNameWithoutExtension(path);
        return Context.Instance != null
            ? Ok(
                new File(
                    name,
                    name,
                    name,
                    fileName,
                    path,
                    null,
                    new Uri(
                        new Uri(ConstPath.LOCAL_INSTANCE_BASE.Replace("{0}", Context.Instance.Id)),
                        path
                    )
                ),
                ResourceType.File
            )
            : Err(ResolveResultError.NotFound);
    }
}
