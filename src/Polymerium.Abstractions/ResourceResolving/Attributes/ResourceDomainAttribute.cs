using System;

namespace Polymerium.Abstractions.ResourceResolving.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ResourceDomainAttribute : Attribute
{
    public ResourceDomainAttribute(string domainName)
    {
        DomainName = domainName;
    }

    public string DomainName { get; }
}
