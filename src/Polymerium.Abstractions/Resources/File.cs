using System;

namespace Polymerium.Abstractions.Resources;

public record File : ResourceBase
{
    public File(string id, string name, string version, string author, Uri? iconSource, Uri? reference, string summary,
        string versionId,
        string fileName,
        string? hash, Uri source) : base(id, name, version, author, iconSource, reference, summary, versionId, null!)
    {
        FileName = fileName;
        Hash = hash;
        Source = source;
    }

    public string FileName { get; set; }
    public string? Hash { get; set; }
    public Uri Source { get; set; }
}