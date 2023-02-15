using System;

namespace Polymerium.Core.Models.Forge;

public struct ForgeLibraryDownloadsArtifact
{
    public string Path { get; set; }
    public Uri Url { get; set; }
    public string Sha1 { get; set; }
    public uint Size { get; set; }
}