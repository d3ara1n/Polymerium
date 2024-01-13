using Trident.Abstractions;

namespace Polymerium.App.Models;

public record LoaderModel(Metadata.Layer.Loader Inner)
{
    public string Name => Metadata.Layer.Loader.MODLOADER_NAME_MAPPINGS.ContainsKey(Inner.Id)
        ? Metadata.Layer.Loader.MODLOADER_NAME_MAPPINGS[Inner.Id]
        : Inner.Id switch
        {
            Metadata.Layer.Loader.COMPONENT_BUILTIN_STORAGE => "Linked Storage",
            _ => Inner.Id
        };

    public string Thumbnail => Inner.Id switch
    {
        Metadata.Layer.Loader.COMPONENT_FORGE => AssetPath.COMPONENTS_FORGE,
        Metadata.Layer.Loader.COMPONENT_NEOFORGE => AssetPath.COMPONENTS_NEOFORGE,
        Metadata.Layer.Loader.COMPONENT_FABRIC => AssetPath.COMPONENTS_FABRIC,
        Metadata.Layer.Loader.COMPONENT_QUILT => AssetPath.COMPONENTS_QUILT,
        _ => AssetPath.PLACEHOLDER_DEFAULT_DIRT
    };
}