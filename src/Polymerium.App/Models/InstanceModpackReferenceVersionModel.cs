using System;

namespace Polymerium.App.Models;

public class InstanceModpackReferenceVersionModel
{
    public InstanceModpackReferenceVersionModel(
        string projectId,
        string versionId,
        string displayName,
        bool isCurrent,
        Uri resource
    )
    {
        ProjectId = projectId;
        VersionId = versionId;
        DisplayName = displayName;
        IsCurrent = isCurrent;
        Resource = resource;
    }

    public string ProjectId { get; set; }
    public string VersionId { get; set; }
    public string DisplayName { get; set; }
    public bool IsCurrent { get; set; }
    public Uri Resource { get; set; }
}
