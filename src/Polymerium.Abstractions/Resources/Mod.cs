using System;

namespace Polymerium.Abstractions.Resources;

public record Mod : ResourceBase
{
    public Mod(
        string id,
        string name,
        string version,
        string author,
        Uri? iconSource,
        Uri? reference,
        string summary,
        string versionId,
        Uri update,
        Uri file
    )
        : base(id, name, version, author, iconSource, reference, summary, versionId, update, file)
    {
    }
}