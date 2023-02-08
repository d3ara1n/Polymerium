using System;

namespace Polymerium.Abstractions.ResourceResolving.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ResourceExpressionAttribute : Attribute
{
    public ResourceExpressionAttribute(string expression)
    {
        Expression = expression;
        // expression compiled as regex object
    }

    public string Expression { get; }
}