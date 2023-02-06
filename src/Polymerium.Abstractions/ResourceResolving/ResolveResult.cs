namespace Polymerium.Abstractions.ResourceResolving;

public class ResolveResult
{
    // 只能是 http, https, file
    public string Url { get; set; }

    public string Hash { get; set; }

    // tagged enum
    public object Description { get; set; }
}