using System;
using System.Collections.Generic;
using System.Linq;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.Models;

public class ExhibitModpackModel(
    string label,
    string? @namespace,
    string projectId,
    string projectName,
    string authorName,
    Uri reference,
    Uri? thumbnail,
    IReadOnlyList<string> tags,
    ulong downloadCount,
    string summary,
    DateTimeOffset createdAt,
    DateTimeOffset updatedAt,
    IReadOnlyList<Uri> gallery) : ModelBase
{
    public static ExhibitModpackModel From(Project project) =>
        new(project.Label,
            project.Namespace,
            project.ProjectId,
            project.ProjectName,
            project.Author,
            project.Reference,
            project.Thumbnail,
            project.Tags,
            project.DownloadCount,
            project.Summary,
            project.CreatedAt,
            project.UpdatedAt,
            [.. project.Gallery.Select(x => x.Url)]);

    #region Direct

    public string ProjectName => projectName;
    public string AuthorName => authorName;
    public string Label => label;
    public string? Namespace => @namespace;
    public string ProjectId => projectId;
    public Uri Reference => reference;
    public Uri? Thumbnail => thumbnail;
    public IReadOnlyList<string> Tags => tags;
    public string Summary => summary;
    public ulong DownloadCount => downloadCount;
    public DateTimeOffset CreatedAt => createdAt;
    public DateTimeOffset UpdatedAt => updatedAt;
    public IReadOnlyList<Uri> Gallery => gallery;

    #endregion
}
