using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;

namespace Polymerium.Core.ResourceResolving;

[ResourceDomain("local")]
[ResourceType("file")]
public class LocalFileResolver : ResourceResolverBase
{
}