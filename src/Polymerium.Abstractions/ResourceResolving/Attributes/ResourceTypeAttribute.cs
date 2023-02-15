using System;

namespace Polymerium.Abstractions.ResourceResolving.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ResourceTypeAttribute : Attribute
{
    public ResourceTypeAttribute(string typeName)
    {
        TypeName = typeName;
    }

    public string TypeName { get; }
}