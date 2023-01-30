using System;
using System.Linq;
using System.Threading.Tasks;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.Models.Game;
using Polymerium.Core.Models.Mojang;
using Wupoo;
using Index = Polymerium.Core.Models.Mojang.Index;

namespace Polymerium.Core.Components.Installers;

public class MinecraftInstaller : ComponentInstallerBase
{
    public override async Task<Result<string>> StartAsync(Component component)
    {
        if (Token.IsCancellationRequested) return Canceled();
        var manifestUrl = "https://piston-meta.mojang.com/mc/game/version_manifest.json";
        VersionManifest? manifest = null;
        await Wapoo.Wohoo(manifestUrl)
            .ForJsonResult<VersionManifest>(x => manifest = x)
            .WhenException<Exception>(e => throw e)
            .FetchAsync();
        if (manifest.HasValue)
        {
            if (manifest.Value.Versions.Any(x => x.Id == component.Version))
            {
                if (Token.IsCancellationRequested) return Canceled();
                var version = manifest.Value.Versions.First(x => x.Id == component.Version);
                Index? index = null;
                await Wapoo.Wohoo(version.Url.AbsoluteUri)
                    .ForJsonResult<Index>(x => index = x)
                    .FetchAsync();
                if (index.HasValue)
                {
                    if (Token.IsCancellationRequested) return Canceled();
                    var assetIndex = new AssetIndex
                    {
                        Id = index.Value.AssetIndex.Id,
                        Sha1 = index.Value.AssetIndex.Sha1,
                        Url = index.Value.AssetIndex.Url.AbsoluteUri
                    };
                    Context.SetAssetIndex(assetIndex);
                    Context.SetJavaVersion((int)index.Value.JavaVersion.MajorVersion);
                    Context.SetMainClass(index.Value.MainClass);
                    foreach (var argument in index.Value.Arguments.Game.Where(x => x.Verfy()))
                    foreach (var value in argument.Values)
                        Context.AddGameArgument(value);

                    foreach (var argument in index.Value.Arguments.Jvm.Where(x => x.Verfy()))
                    foreach (var value in argument.Values)
                        Context.AddJvmArguments(value);

                    foreach (var item in index.Value.Libraries.Where(x => x.Verfy()))
                    {
                        var library = new Library
                        {
                            IsNative = false,
                            Name = item.Name,
                            Sha1 = item.Downloads.Artifact.Sha1,
                            Url = item.Downloads.Artifact.Url,
                            Path = item.Downloads.Artifact.Path
                        };
                        Context.AppendLibrary(library);
                        if (item.Natives.HasValue)
                        {
                            var name = item.Natives.Value.Windows;
                            var classifier = item.Downloads.Classifiers.First(x => x.Identity == name);
                            var native = new Library
                            {
                                IsNative = true,
                                Name = item.Name,
                                Path = classifier.Path,
                                Sha1 = classifier.Sha1,
                                Url = classifier.Url
                            };
                            Context.AppendLibrary(native);
                        }
                    }

                    var client = new Library
                    {
                        IsNative = false,
                        Name = $"net/minecraft:minecraft:{component.Version}",
                        Path =
                            $"net/minecraft/minecraft/{component.Version}/minecraft/minecraft-{component.Version}.jar",
                        Sha1 = index.Value.Downloads.Client.Sha1,
                        Url = index.Value.Downloads.Client.Url
                    };
                    Context.AppendLibrary(client);
                    return Finished();
                }

                return Failed("版本索引文件下载失败");
            }

            return Failed($"要求的版本 {component.Version} 不在清单之中");
        }

        return Failed("清单文件下载失败");
    }
}