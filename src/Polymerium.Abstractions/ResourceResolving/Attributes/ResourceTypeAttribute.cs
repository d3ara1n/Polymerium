using System;
using Polymerium.Abstractions.Resources;

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
