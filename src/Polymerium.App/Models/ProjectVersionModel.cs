using System.Linq;
using Trident.Abstractions;
using Trident.Abstractions.Resources;
using static Trident.Abstractions.Metadata.Layer;

namespace Polymerium.App.Models
{
    public record ProjectVersionModel(Project.Version Inner, ProjectModel Model)
    {
        public string RequiredAnyOfVersions => string.Join("·", Inner.Requirements.AnyOfVersions);
        public string RequiredAnyOfLoaders => string.Join("·", Inner.Requirements.AnyOfLoaders.Select(x => Loader.MODLOADER_NAME_MAPPINGS.Keys.Contains(x) ? Loader.MODLOADER_NAME_MAPPINGS[x] : x));
        public string Labels => string.Join("·", RequiredAnyOfLoaders, RequiredAnyOfVersions);
    }
}
