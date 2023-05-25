using System;
using System.Collections.Generic;

namespace Polymerium.Abstractions.Resources;

public record Update : ResourceBase
{
    public Update(
        string id,
        string name,
        string version,
        string versionId,
        IEnumerable<Uri> versions
    )
        : base(id, name, version, string.Empty, null, null, string.Empty, versionId, null, null)
    {
        Versions = versions;
    }

    public IEnumerable<Uri> Versions { get; set; }
}
