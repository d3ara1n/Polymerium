﻿using System.Windows.Input;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models;

public record LoaderModel(Loader Inner, ICommand RemoveCommand)
{
    public string Name => Loader.MODLOADER_NAME_MAPPINGS.ContainsKey(Inner.Identity)
        ? Loader.MODLOADER_NAME_MAPPINGS[Inner.Identity]
        : Inner.Identity switch
        {
            Loader.COMPONENT_BUILTIN_STORAGE => "Linked Storage",
            _ => Inner.Identity
        };

    public string Thumbnail => Inner.Identity switch
    {
        Loader.COMPONENT_FORGE => AssetPath.COMPONENTS_FORGE,
        Loader.COMPONENT_NEOFORGE => AssetPath.COMPONENTS_NEOFORGE,
        Loader.COMPONENT_FABRIC => AssetPath.COMPONENTS_FABRIC,
        Loader.COMPONENT_QUILT => AssetPath.COMPONENTS_QUILT,
        _ => AssetPath.PLACEHOLDER_DEFAULT_DIRT
    };
}