using System;
using DotNext.Threading;

namespace Polymerium.Abstractions.Resources;

public record Modpack : ResourceBase
{
    public Modpack(string id, string name, string author, Uri iconSource, string summary,
        AsyncLazy<string> body) : base(id,
        name, author, iconSource, summary, body)
    {
    }
}