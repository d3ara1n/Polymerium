using System;

namespace Polymerium.Core.Models.Mojang.Indexes;

public struct AssetIndex
{
    public string Id { get; set; }
    public string Sha1 { get; set; }
    public uint Size { get; set; }
    public uint TotalSize { get; set; }
    public Uri Url { get; set; }
}