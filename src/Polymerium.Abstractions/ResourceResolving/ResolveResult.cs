using System;

namespace Polymerium.Abstractions.ResourceResolving;

public class ResolveResult
{
    public ResolveResult(Uri source, Uri path, string? hash, object? description = null)
    {
        Source = source;
        Hash = hash;
        Path = path;
        Description = description;
    }

    // 只能是 http, https, file
    public Uri Source { get; set; }

    public string? Hash { get; set; }

    // destination: poly-file
    public Uri Path { get; set; }

    // tagged enum
    public object? Description { get; set; }
}