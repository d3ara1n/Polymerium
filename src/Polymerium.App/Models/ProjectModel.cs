using Humanizer;
using System.Collections.Generic;
using System.Linq;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models;

public record ProjectModel(Project Inner, Filter Filter)
{
    public string Thumbnail => (Inner.Thumbnail?.IsAbsoluteUri ?? false ? Inner.Thumbnail?.AbsoluteUri : null) ??
                               AssetPath.PLACEHOLDER_DEFAULT_DIRT;

    public string CreatedAt => Inner.CreatedAt.Humanize();
    public string UpdatedAt => Inner.UpdatedAt.Humanize();
    public string DownloadCount => ((int)Inner.DownloadCount).ToMetric(decimals: 2);

    public IList<GalleryItemModel> Gallery =>
        Inner.Gallery.Select(x => new GalleryItemModel(x.Title, x.Url)).ToList();

    public IList<ProjectVersionModel> Versions =>
        Inner.Versions.Select(x => new ProjectVersionModel(x, this, IsMatched(x, Filter))).ToList();

    private bool IsMatched(Project.Version version, Filter filter) =>
        (!version.Requirements.AnyOfLoaders.Any() || (filter.ModLoader != null &&
                                                      version.Requirements.AnyOfLoaders.Contains(
                                                          filter.ModLoader))) && filter.Version != null &&
        version.Requirements.AnyOfVersions.Contains(filter.Version);
}