using System;

namespace Polymerium.Abstractions.Resources;

public record Mod : ResourceBase
{
    public Mod(string id, string name, string author, Uri? iconSource, string summary, string version, Uri file) : base(
        id, name, author, iconSource, summary, version, file)
    {
    }
}