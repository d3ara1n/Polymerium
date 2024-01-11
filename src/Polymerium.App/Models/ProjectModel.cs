using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models;

public record ProjectModel(Project Inner, string RepositoryLabel)
{
    public static readonly ProjectModel DUMMY =
        new(
            new Project(string.Empty, string.Empty, null, string.Empty, string.Empty, new Uri("https://example.com"),
                ResourceKind.Mod, DateTimeOffset.Now, DateTimeOffset.Now, 0, string.Empty,
                Enumerable.Empty<Project.Screenshot>(), Enumerable.Empty<Project.Version>()), string.Empty);

    public string Thumbnail => Inner.Thumbnail?.AbsoluteUri ?? "/Assets/Placeholders/default_dirt.png";
    public string CreatedAt => Inner.CreatedAt.Humanize();
    public string UpdatedAt => Inner.UpdatedAt.Humanize();
    public string DownloadCount => ((int)Inner.DownloadCount).ToMetric(decimals: 2);

    public IEnumerable<GalleryItemModel> Gallery =>
        Inner.Gallery.Select(x => new GalleryItemModel(x.Title, x.Url)).ToList();

    public IEnumerable<ProjectVersionModel> Versions => Inner.Versions.Select(x => new ProjectVersionModel(x, this));
}