using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.Models.Game;
using Polymerium.Core.Models.Mojang;
using System;
using System.Linq;
using System.Threading.Tasks;
using Wupoo;
using Index = Polymerium.Core.Models.Mojang.Index;

namespace Polymerium.Core.Components.Installers;

public sealed class MinecraftComponentInstaller : ComponentInstallerBase
{
    public override async Task<ComponentInstallerError?> StartAsync(Component component)
    {
        if (Token.IsCancellationRequested)
            return Canceled();
        var manifestUrl = "https://piston-meta.mojang.com/mc/game/version_manifest.json";
        VersionManifest? manifest = null;
        await Wapoo
            .Wohoo(manifestUrl)
            .ForJsonResult<VersionManifest>(x => manifest = x)
            .WhenException<Exception>(e => throw e)
            .FetchAsync();
        if (manifest.HasValue)
        {
            if (manifest.Value.Versions.Any(x => x.Id == component.Version))
            {
                if (Token.IsCancellationRequested)
                    return Canceled();
                var version = manifest.Value.Versions.First(x => x.Id == component.Version);
                Index? index = null;
                await Wapoo
                    .Wohoo(version.Url.AbsoluteUri)
                    .ForJsonResult<Index>(x => index = x)
                    .FetchAsync();
                if (index.HasValue)
                {
                    if (Token.IsCancellationRequested)
                        return Canceled();
                    var assetIndex = new AssetIndex
                    {
                        Id = index.Value.AssetIndex.Id,
                        Sha1 = index.Value.AssetIndex.Sha1,
                        Url = index.Value.AssetIndex.Url.AbsoluteUri
                    };
                    Context.SetAssetIndex(assetIndex);
                    Context.SetJavaVersion((int)index.Value.JavaVersion.MajorVersion);
                    Context.SetMainClass(index.Value.MainClass);
                    // compliance 的等级影响 arguments 存在的形式
                    if (index.Value.Arguments.HasValue)
                    {
                        foreach (
                            var argument in index.Value.Arguments.Value.Game
                                .Where(x => x.Verify())
                                .SelectMany(x => x.Values)
                        )
                            Context.AppendGameArgument(argument);

                        foreach (
                            var argument in index.Value.Arguments.Value.Jvm
                                .Where(x => x.Verify())
                                .SelectMany(x => x.Values)
                        )
                            Context.AppendJvmArguments(argument);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(index.Value.MinecraftArguments))
                            foreach (var split in index.Value.MinecraftArguments.Split())
                                Context.AppendGameArgument(split);
                        // patch the jvm arguments (copied from 1.19.3's jvm arguments)
                        Context.AppendJvmArguments("-Djava.library.path=${natives_directory}");
                        Context.AppendJvmArguments("-Dminecraft.launcher.brand=${launcher_name}");
                        Context.AppendJvmArguments(
                            "-Dminecraft.launcher.version=${launcher_version}"
                        );
                        Context.AppendJvmArguments("-cp");
                        Context.AppendJvmArguments("${classpath}");
                        Context.AddCrate("user_properties", "{}");
                    }

                    foreach (var item in index.Value.Libraries.Where(x => x.Verify()))
                    {
                        if (item.Downloads.Artifact.HasValue)
                        {
                            var library = new Library(
                                item.Name,
                                item.Downloads.Artifact.Value.Path,
                                item.Downloads.Artifact.Value.Sha1,
                                item.Downloads.Artifact.Value.Url
                            );
                            Context.AddLibrary(library);
                        }

                        if (item.Natives.HasValue)
                        {
                            // 就不能在 natives 里分出个架构版本，偏要插值，难不难受啊
                            var name = item.Natives.Value.Windows.Replace(
                                "${arch}",
                                Environment.Is64BitOperatingSystem ? "64" : "32"
                            );
                            var classifier = item.Downloads.Classifiers.First(
                                x => x.Identity == name
                            );
                            var native = new Library(
                                item.Name,
                                classifier.Path,
                                classifier.Sha1,
                                classifier.Url,
                                true
                            );
                            Context.AddLibrary(native);
                        }
                    }

                    var client = new Library(
                        $"net.minecraft:minecraft:{component.Version}",
                        $"net/minecraft/minecraft/{component.Version}/minecraft-{component.Version}.jar",
                        index.Value.Downloads.Client.Sha1,
                        index.Value.Downloads.Client.Url
                    );
                    Context.AddLibrary(client);
                    return Finished();
                }

                return Failed(ComponentInstallerError.NetworkError);
            }

            return Failed(ComponentInstallerError.ResourceNotFound);
        }

        return Failed(ComponentInstallerError.NetworkError);
    }
}
