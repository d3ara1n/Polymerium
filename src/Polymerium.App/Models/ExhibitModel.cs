using System;
using System.Collections.Generic;
using Humanizer;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class ExhibitModel(
    string label,
    string? ns,
    string projectId,
    string projectName,
    string summary,
    Uri thumbnail,
    string author,
    IReadOnlyList<string> tags,
    DateTimeOffset updatedAt,
    ulong downloads,
    Uri reference) : ModelBase
{
    #region Direct

    public string Label => label;

    public string? Ns => ns;

    public string ProjectId => projectId;

    public string ProjectName => projectName;

    public string Summary => summary;

    public Uri Thumbnail => thumbnail;

    public string Author => author;

    public IReadOnlyList<string> Tags => tags;

    public string UpdatedAt => updatedAt.Humanize();

    public string Downloads => ((double)downloads).ToMetric(decimals: 2);

    public Uri Reference => reference;

    #endregion
}