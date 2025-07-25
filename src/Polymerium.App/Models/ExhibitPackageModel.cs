﻿using System;
using System.Collections.Generic;
using Humanizer;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class ExhibitPackageModel(
    string label,
    string? @namespace,
    string projectId,
    string projectName,
    string authorName,
    Uri? reference,
    Uri? thumbnail,
    IReadOnlyList<string> tags,
    ulong downloadCount,
    string summary,
    DateTimeOffset updatedAt,
    IReadOnlyList<Uri> gallery) : ModelBase
{
    #region Direct

    public string Label => label;
    public string? Namespace => @namespace;
    public string ProjectId => projectId;
    public string ProjectName => projectName;
    public string AuthorName => authorName;
    public Uri? Reference => reference;
    public Uri Thumbnail => thumbnail ?? AssetUriIndex.DIRT_IMAGE;
    public IReadOnlyList<string> Tags => tags;
    public string Summary => summary;
    public ulong DownloadCountRaw => downloadCount;
    public string DownloadCount => ((int)downloadCount).ToMetric(decimals: 2);
    public DateTimeOffset UpdatedAtRaw => updatedAt;
    public string UpdatedAt => updatedAt.Humanize();
    public IReadOnlyList<Uri> Gallery => gallery;

    #endregion
}