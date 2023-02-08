using System;

namespace Polymerium.Abstractions.ResourceResolving;

public class ResolveResult
{
    // 只能是 http, https, file
    public ResolveResult(Uri url, string hash, Uri path, object? description = null)
    {
        Url = url;
        Hash = hash;
        Path = path;
        Description = description;
    }

    public Uri Url { get; set; }

    public string Hash { get; set; }

    // destination: poly-file
    public Uri Path { get; set; }

    // tagged enum
    public object? Description { get; set; }
}