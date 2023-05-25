using System;
using System.Text.RegularExpressions;

namespace Polymerium.Abstractions.ResourceResolving.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ResourceExpressionAttribute : Attribute
{
    // {id}/{version}
    private static readonly Regex PATTERN = new(@"\{(?<name>\*?[0-9a-zA-Z_-]+)\}");

    public ResourceExpressionAttribute(string expression)
    {
        Expression = expression;
        // expression compiled as regex object
        var newPattern = PATTERN.Replace(
            expression,
            m =>
                m.Value.Contains("*")
                    ? $"(?<{m.Groups["name"].Value.Replace("*", "")}>[0-9a-zA-Z_.,'~!$&%#@^\\(\\)\\\\[\\] \"\\/+-]+)"
                    : $"(?<{m.Groups["name"].Value}>[0-9a-zA-Z_.,'~!$&%#@^\\(\\)\\\\[\\] \"+-]+)"
        );
        Compiled = new Regex($"^{newPattern}$");
    }

    public string Expression { get; }
    public Regex Compiled { get; }
}
