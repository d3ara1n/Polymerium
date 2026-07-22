using System;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class InstanceReferenceModel(
    string pref,
    string label,
    string projectName,
    string versionId,
    string versionName,
    Uri? thumbnail,
    Uri sourceUrl) : ModelBase
{
    #region Direct

    public string Pref => pref;
    public string Label => label;
    public string ProjectName => projectName;
    public string VersionId => versionId;
    public string VersionName => versionName;
    public Uri? Thumbnail => thumbnail;
    public Uri SourceUrl => sourceUrl;

    #endregion
}
