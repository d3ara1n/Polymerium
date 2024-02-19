using Polymerium.Trident.Engines.Launching;
using Polymerium.Trident.Services;
using System;
using Trident.Abstractions.Building;

namespace Polymerium.Trident.Extensions;

public static class ArtifactExtensions
{
    public static Igniter MakeIgniter(this Artifact self, TridentContext context)
    {
        var igniter = new Igniter();

        foreach (var argument in self.GameArguments)
            igniter.AddGameArgument(argument);
        foreach (var argument in self.JvmArguments)
            igniter.AddJvmArgument(argument);
        foreach (var library in self.Libraries.Where(x => x.IsPresent))
            igniter.AddLibrary(context.LibraryPath(library.Id.Namespace, library.Id.Name, library.Id.Version, library.Id.Platform));

        igniter
            .SetMainClass(self.MainClass)
            .SetAssetIndex(self.AssetIndex.Id);

        return igniter;
    }
}
