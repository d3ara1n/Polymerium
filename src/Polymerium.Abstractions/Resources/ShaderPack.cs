using System;

namespace Polymerium.Abstractions.Resources;

public record ShaderPack : ResourceBase
{
    public ShaderPack(string id, string version, string name, string author, Uri? iconSource, Uri? reference,
        string summary,
        string versionId,
        Uri file) : base(
        id, name, version, author, iconSource, reference, summary, versionId, file)
    {
    }
}