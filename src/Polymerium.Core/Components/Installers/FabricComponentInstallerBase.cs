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
        IEnumerable<FabricVersion>? versions = null;
        await Wapoo.Wohoo(new Uri(ManifestUrl, Context.GetCoreVersion()).AbsoluteUri)
            .ForJsonResult<IEnumerable<FabricVersion>>(x => versions = x)
            .FetchAsync();
        if (versions != null)
        {
            var fabricVersions = versions as FabricVersion[] ?? versions.ToArray();
            if (fabricVersions.Any(x => x.Loader.Version == component.Version))
            {
                var version = fabricVersions.First(x => x.Loader.Version == component.Version);
                var loaderPath = MavenToPath(version.Loader.Maven);
                var intermediaryPath = MavenToPath(version.Intermediary.Maven);
                var loader = new Library(version.Loader.Maven, loaderPath, null, new Uri(MavenUrl, loaderPath));
                var intermediary = new Library(version.Intermediary.Maven, intermediaryPath, null,
                    new Uri(MavenUrl, intermediaryPath));
                foreach (var item in
                         version.LauncherMeta.Libraries.Common.Concat(version.LauncherMeta.Libraries.Client))
                {
                    var path = MavenToPath(item.Name);
                    var library = new Library(item.Name, path, null, new Uri(item.Url, path));
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