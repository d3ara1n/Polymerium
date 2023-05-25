using System;

namespace Polymerium.Core.Models.Mojang.Indexes;

public struct IndexLoggingItemFile
{
    public string Id { get; set; }
    public string Sha1 { get; set; }
    public uint Size { get; set; }
    public Uri Url { get; set; }
}
