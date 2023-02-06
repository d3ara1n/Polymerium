using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.Models.Game;
using Polymerium.Core.Models.Fabric;
using Wupoo;

namespace Polymerium.Core.Components.Installers;

public abstract class FabricComponentInstallerBase : ComponentInstallerBase
{
    protected abstract Uri MavenUrl { get; }
    protected abstract Uri ManifestUrl { get; }

    public override async Task<Result<string>> StartAsync(Component component)
    {
        if (Token.IsCancellationRequested) return Canceled();
        IEnumerable<FabricVersion> versions = null;
        await Wapoo.Wohoo(new Uri(ManifestUrl, Context.GetCoreVersion()).AbsoluteUri)
            .ForJsonResult<IEnumerable<FabricVersion>>(x => versions = x)
            .FetchAsync();
        if (versions != null)
        {
            if (versions.Any(x => x.Loader.Version == component.Version))
            {
                var version = versions.First(x => x.Loader.Version == component.Version);
                var loaderPath = MavenToPath(version.Loader.Maven);
                var intermediaryPath = MavenToPath(version.Intermediary.Maven);
                var loader = new Library
                {
                    Name = version.Loader.Maven,
                    Path = loaderPath,
                    Url = new Uri(MavenUrl, loaderPath),
                    Sha1 = null,
                    IsNative = false
                };
                var intermediary = new Library
                {
                    Name = version.Intermediary.Maven,
                    Path = intermediaryPath,
                    Url = new Uri(MavenUrl, intermediaryPath),
                    Sha1 = null,
                    IsNative = false
                };
                foreach (var item in
                         version.LauncherMeta.Libraries.Common.Concat(version.LauncherMeta.Libraries.Client))
                {
                    var path = MavenToPath(item.Name);
                    var library = new Library
                    {
                        Name = item.Name,
                        Path = path,
                        Url = new Uri(item.Url, path),
                        Sha1 = null,
                        IsNative = false
                    };
                    Context.AddLibrary(library);
                }

                Context.AddLibrary(loader);
                Context.AddLibrary(intermediary);

                Context.SetMainClass(version.LauncherMeta.MainClass.Client);
                return Finished();
            }

            return Failed("Version not found in manifest");
        }

        return Failed("Network error or version not compatible");
    }

    private string MavenToPath(string maven, string separator = ".")
    {
        var splits = maven.Split(':');
        var org = splits[0];
        var name = splits[1];
        var version = splits[2];
        return $"{org.Replace(separator, "/")}/{name}/{version}/{name}-{version}.jar";
    }
}