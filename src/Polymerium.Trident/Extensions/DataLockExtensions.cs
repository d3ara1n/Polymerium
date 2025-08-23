using Polymerium.Trident.Igniters;
using Trident.Abstractions;
using Trident.Abstractions.FileModels;

namespace Polymerium.Trident.Extensions
{
    public static class DataLockExtensions
    {
        public static Igniter MakeIgniter(this DataLock self)
        {
            Igniter igniter = new();

            foreach (var argument in self.GameArguments)
            {
                igniter.AddGameArgument(argument);
            }

            foreach (var argument in self.JavaArguments)
            {
                igniter.AddJvmArgument(argument);
            }

            foreach (var library in self.Libraries.Where(x => x.IsPresent))
            {
                igniter.AddLibrary(PathDef.Default.FileOfLibrary(library.Id.Namespace,
                                                                 library.Id.Name,
                                                                 library.Id.Version,
                                                                 library.Id.Platform,
                                                                 library.Id.Extension));
            }

            igniter.SetMainClass(self.MainClass).SetAssetIndex(self.AssetIndex.Id);

            return igniter;
        }
    }
}
