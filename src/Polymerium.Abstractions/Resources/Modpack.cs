using System;

namespace Polymerium.Abstractions.Resources;

public record Modpack : ResourceBase
{
    public Modpack(string id, string name, string author, Uri iconSource, string summary) : base(id,
        name, author, iconSource, summary)
    {
    }
}