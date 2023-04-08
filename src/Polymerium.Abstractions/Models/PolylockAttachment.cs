using System;

namespace Polymerium.Abstractions.Models;

public struct PolylockAttachment
{
    public string CachedObjectPath { get; set; }
    public Uri Source { get; set; }
    public string? Sha1 { get; set; }
    public Uri Target { get; set; }
}