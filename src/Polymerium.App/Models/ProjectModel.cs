using Humanizer;
using System.Collections.Generic;
using System.Linq;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models
{
    public record ProjectModel(Project Inner, IRepository Repository)
    {
        public string Thumbnail => Inner.Thumbnail?.AbsoluteUri ?? "/Assets/Placeholders/default_dirt.png";
        public string CreatedAt => Inner.CreatedAt.Humanize();
        public string UpdatedAt => Inner.UpdatedAt.Humanize();
        public string DownloadCount => ((int)Inner.DownloadCount).ToMetric(decimals: 2);

        public IEnumerable<GalleryItemModel> Gallery => Inner.Gallery.Select(x => new GalleryItemModel(x.Title, x.Url)).ToList();
        public IEnumerable<ProjectVersionModel> Versions => Inner.Versions.Select(x => new ProjectVersionModel(x, this));

    }
}
