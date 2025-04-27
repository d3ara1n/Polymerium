using System;
using System.Collections.Generic;
using Humanizer;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class ExhibitModpackModel(
    string displayName,
    string authorName,
    string referenceLabel,
    Uri referenceUrl,
    IReadOnlyList<string> tags,
    ulong downloadCount,
    string summary,
    string description,
    DateTimeOffset updatedAt,
    IReadOnlyList<Uri> gallery,
    IReadOnlyList<ExhibitVersionModel> versions) : ModelBase
{
    #region Direct

    public string DisplayName => displayName;
    public string AuthorName => authorName;
    public string ReferenceLabel => referenceLabel;
    public Uri ReferenceUrl => referenceUrl;
    public IReadOnlyList<string> Tags => tags;
    public string Summary => summary;
    public string Description => description;
    public ulong DownloadCountRaw => downloadCount;
    public string DownloadCount => ((int)downloadCount).ToMetric(decimals: 2);
    public DateTimeOffset UpdatedAtRaw => updatedAt;
    public string UpdatedAt => updatedAt.Humanize();
    public IReadOnlyList<Uri> Gallery => gallery;
    public IReadOnlyList<ExhibitVersionModel> Versions => versions;

    #endregion
}