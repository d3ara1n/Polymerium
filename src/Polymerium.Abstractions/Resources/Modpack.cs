using System;

namespace Polymerium.Abstractions.Resources;

public record Modpack : ResourceBase
{
    public Modpack(string id, string name, string author, Uri? iconSource, Uri? reference, string summary,
        string version,
        Uri file) : base(id,
        name, author, iconSource, reference, summary, version, file)
    {
    }
}