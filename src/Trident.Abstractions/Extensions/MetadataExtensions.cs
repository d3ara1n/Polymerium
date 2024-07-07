﻿using Trident.Abstractions.Resources;

namespace Trident.Abstractions.Extensions;

public static class MetadataExtensions
{
    public static string? ExtractModLoader(this Metadata self) =>
        self.Layers.SelectMany(x => x.Loaders)
            .FirstOrDefault(x => Loader.MODLOADER_NAME_MAPPINGS.ContainsKey(x.Identity))?.Identity;
}