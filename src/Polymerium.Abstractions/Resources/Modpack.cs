﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Polymerium.Abstractions.Resources;

public record Modpack : ResourceBase
{
    public Modpack(
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
    { }
}
