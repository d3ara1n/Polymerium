using Polymerium.Trident.Engines.Launching;
using Polymerium.Trident.Services;
using Trident.Abstractions.Building;

namespace Polymerium.Trident.Extensions
{
    public static class ArtifactExtensions
    {
        public static Igniter MakeIgniter(this Artifact self, TridentContext context)
        {
            Igniter igniter = new();

            foreach (string argument in self.GameArguments)
            {
                igniter.AddGameArgument(argument);
            }

            foreach (string argument in self.JvmArguments)
            {
                igniter.AddJvmArgument(argument);
            }

            foreach (Artifact.Library library in self.Libraries.Where(x => x.IsPresent))
            {
                igniter.AddLibrary(context.LibraryPath(library.Id.Namespace, library.Id.Name, library.Id.Version,
                    library.Id.Platform, library.Id.Extension));
            }

            igniter
                .SetMainClass(self.MainClass)
                .SetAssetIndex(self.AssetIndex.Id);

            return igniter;
        }
    }
}