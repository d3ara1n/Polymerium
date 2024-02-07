using System.Linq;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Extensions;

public static class ProfileExtensions
{
    public static string ExtractTypeDisplay(this Profile self)
    {
        var modloader = self.Metadata.Layers.SelectMany(x => x.Loaders)
            .FirstOrDefault(x => Loader.MODLOADER_NAME_MAPPINGS.Keys.Contains(x.Id));
        return modloader != null ? Loader.MODLOADER_NAME_MAPPINGS[modloader.Id] : "Vanilla";
    }
}