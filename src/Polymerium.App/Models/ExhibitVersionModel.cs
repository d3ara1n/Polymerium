using System;
using Humanizer;
using Polymerium.App.Facilities;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public class ExhibitVersionModel(
    string label,
    string? @namespace,
    string projectName,
    string projectId,
    string versionName,
    string versionid,
    string changelog,
    DateTimeOffset publishedAt,
    ulong downloadCount,
    ReleaseType type,
    string purl) : ModelBase
{
    #region Direct

    public string Label => label;
    public string? Namespace => @namespace;
    public string ProjectName => projectName;
    public string ProjectId => projectId;
    public string VersionName => versionName;
    public string Versionid => versionid;
    public string Changelog => changelog;
    public DateTimeOffset PublishedAtRaw => publishedAt;
    public string PublishedAt => publishedAt.Humanize();
    public ulong DownloadCount => downloadCount;
    public ReleaseType TypeRaw => type;
    public string Purl => purl;

    #endregion
}