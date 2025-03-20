using System;
using Humanizer;
using Polymerium.App.Facilities;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public class ExhibitVersionModel(
    string displayName,
    string vid,
    string changelog,
    DateTimeOffset publishedAt,
    ulong downloadCount,
    ReleaseType type,
    string purl) : ModelBase
{
    #region Direct

    public string DisplayName => displayName;
    public string Version => vid;
    public string Changelog => changelog;
    public DateTimeOffset PublishedAtRaw => publishedAt;
    public string PublishedAt => publishedAt.Humanize();
    public ulong DownloadCount => downloadCount;
    public ReleaseType TypeRaw => type;
    public string Purl => purl;

    #endregion
}