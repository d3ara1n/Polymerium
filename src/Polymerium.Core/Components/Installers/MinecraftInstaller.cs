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
                    // compliance 的等级影响 arguments 存在的形式
                    if (index.Value.ComplianceLevel == 1 && index.Value.Arguments.HasValue)
                    {
                        foreach (var argument in index.Value.Arguments.Value.Game.Where(x => x.Verfy())
                                     .SelectMany(x => x.Values))
                            Context.AddGameArgument(argument);

                        foreach (var argument in index.Value.Arguments.Value.Jvm.Where(x => x.Verfy())
                                     .SelectMany(x => x.Values))
                            Context.AddJvmArguments(argument);
                    }

                    if (index.Value.ComplianceLevel == 0)
                    {
                        if (!string.IsNullOrEmpty(index.Value.MinecraftArguments))
                            foreach (var split in index.Value.MinecraftArguments.Split())
                                Context.AddGameArgument(split);
                        // patch the jvm arguments (copied from 1.19.3's jvm arguments)
                        Context.AddJvmArguments("-Djava.library.path=${natives_directory}");
                        Context.AddJvmArguments("-Dminecraft.launcher.brand=${launcher_name}");
                        Context.AddJvmArguments("-Dminecraft.launcher.version=${launcher_version}");
                        Context.AddJvmArguments("-cp");
                        Context.AddJvmArguments("${classpath}");
                        Context.AddCrate("user_properties", "{}");
                    }

                    foreach (var item in index.Value.Libraries.Where(x => x.Verfy()))
                    {
                        if (item.Downloads.Artifact.HasValue)
                        {
                            var library = new Library
                            {
                                IsNative = false,
                                Name = item.Name,
                                Sha1 = item.Downloads.Artifact.Value.Sha1,
                                Url = item.Downloads.Artifact.Value.Url,
                                Path = item.Downloads.Artifact.Value.Path
                            };
                            Context.AppendLibrary(library);
                        }

                        if (item.Natives.HasValue)
                        {
                            // 就不能在 natives 里分出个架构版本，偏要插值，难不难受啊
                            var name = item.Natives.Value.Windows.Replace("${arch}",
                                Environment.Is64BitOperatingSystem ? "64" : "32");
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