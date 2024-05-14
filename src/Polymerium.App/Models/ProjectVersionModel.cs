using Humanizer;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models
{
    public record ProjectVersionModel(Project.Version Inner, ProjectModel Root, bool Matched)
    {
        public string PublishedAt => Inner.PublishedAt.Humanize();

        public string LoaderLabel => string.Join(',', Inner.Requirements.AnyOfLoaders.Select(x =>
            Loader.MODLOADER_NAME_MAPPINGS.Keys.Contains(x) ? Loader.MODLOADER_NAME_MAPPINGS[x] : x));

        public string VersionLabel => string.Join(',', Inner.Requirements.AnyOfVersions);
    }
}