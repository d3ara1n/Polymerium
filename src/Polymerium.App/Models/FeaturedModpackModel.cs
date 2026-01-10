using System;
using System.Collections.Generic;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class FeaturedModpackModel(
    string label,
    string? @namespace,
    string projectId,
    string projectName,
    string author,
    Uri thumbnail,
    IReadOnlyList<string> tags) : ModelBase
{
    #region Direct

    public string Label => label;
    public string? Namespace => @namespace;
    public string ProjectId => projectId;
    public string ProjectName => projectName;
    public string Author => author;
    public Uri Thumbnail => thumbnail;
    public IReadOnlyList<string> Tags => tags;

    #endregion
}
