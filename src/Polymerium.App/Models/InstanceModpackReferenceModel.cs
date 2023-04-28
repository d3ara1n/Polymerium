using System;
using System.Collections;
using System.Collections.Generic;

namespace Polymerium.App.Models;

public class InstanceModpackReferenceModel
{
    public InstanceModpackReferenceModel(
        string name,
        string id,
        string version,
        string versionId,
        string author,
        string summary,
        IEnumerable<InstanceModpackReferenceVersionModel> versions,
        Uri reference
    )
    {
        Name = name;
        Id = id;
        Version = version;
        VersionId = versionId;
        Author = author;
        Summary = summary;
        Versions = versions;
        Reference = reference;
    }

    public string Name { get; set; }
    public string Id { get; set; }
    public string Version { get; set; }
    public string VersionId { get; set; }
    public string Author { get; set; }
    public string Summary { get; set; }
    public IEnumerable<InstanceModpackReferenceVersionModel> Versions { get; set; }
    public Uri Reference { get; set; }
}
