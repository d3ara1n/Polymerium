using Polymerium.Abstractions.Resources;
using System;

namespace Polymerium.Abstractions.ResourceResolving.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ResourceTypeAttribute : Attribute
{
    public ResourceTypeAttribute(ResourceType type)
    {
        ResourceType = type;
    }

    public ResourceType ResourceType { get; }
}
