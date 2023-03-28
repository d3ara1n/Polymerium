using System;

namespace Polymerium.Abstractions.Resources;

public record ResourcePack : ResourceBase
{
    public ResourcePack(string id, string name, string version, string author, Uri? iconSource, Uri? reference,
        string summary,
        string versionId,
        Uri file) : base(
        id, name, version, author, iconSource, reference, summary, versionId, file)
    {
    }
}