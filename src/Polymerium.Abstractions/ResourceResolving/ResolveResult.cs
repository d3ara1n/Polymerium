using Polymerium.Abstractions.Resources;

namespace Polymerium.Abstractions.ResourceResolving;

public class ResolveResult
{
    public ResolveResult(ResourceBase resource, ResourceType type)
    {
        Resource = resource;
        Type = type;
    }

    public ResourceBase Resource { get; set; }
    public ResourceType Type { get; set; }
}