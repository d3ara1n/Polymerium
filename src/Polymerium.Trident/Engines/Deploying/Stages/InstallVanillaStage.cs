using Microsoft.Extensions.Logging;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Models.PrismLauncher;
using Trident.Abstractions.Building;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Extensions;

namespace Polymerium.Trident.Engines.Deploying.Stages
{
    public class InstallVanillaStage(IHttpClientFactory factory) : StageBase
    {
        protected override async Task OnProcessAsync()
        {
            ArtifactBuilder builder = Context.ArtifactBuilder!;
            using HttpClient client = factory.CreateClient();
            PrismIndex manifest =
                await PrismLauncherHelper.GetManifestAsync(PrismLauncherHelper.UID_MINECRAFT, factory, Context.Token);
            Logger.LogInformation("Got manifest with {count} entries", manifest.Versions.Length);
            PrismIndexVersion index = manifest.Versions.FirstOrDefault(x => x.Version == Context.Metadata.Version);
            if (index.Equals(default))
            {
                throw new BadFormatException("{minecraft_manifest}", $"versions[version:{Context.Metadata.Version}]");
            }

            PrismVersion version = await PrismLauncherHelper.GetVersionAsync(PrismLauncherHelper.UID_MINECRAFT,
                index.Version, factory, Context.Token);
            Logger.LogInformation("Got version index {version}({uid})", version.Version, version.Uid);

            // Libraries
            IEnumerable<PrismVersionLibrary> patched =
                await PrismLauncherHelper.GetPatchedLibraries(version, factory, Context.Token);
            PrismLauncherHelper.AddValidatedLibrariesToArtifact(builder, patched);

            Logger.LogInformation("Libraries added, refer to artifact file for details");

            // Main Jar as a Library as well
            if (version.MainJar != null && version.MainJar.Value.Downloads.Artifact.HasValue)
            {
                builder.AddLibrary(version.MainJar.Value.Name, version.MainJar.Value.Downloads.Artifact.Value.Url,
                    version.MainJar.Value.Downloads.Artifact.Value.Sha1);
            }
            else
            {
                throw new BadFormatException("{minecraft_version}", "mainJar.downloads.artifact");
            }

            Logger.LogInformation("Client jar appended: {name}", version.MainJar.Value.Name);

            // Game Arguments
            IEnumerable<string> arguments =
                version.MinecraftArguments?.Split(' ') ?? Enumerable.Empty<string>();
            foreach (string? arg in arguments)
            {
                builder.AddGameArgument(arg);
            }

            Logger.LogInformation("Game arguments added, refer to artifact file for details");

            // Jvm Arguments
            string[] jvmArguments =
            [
                // 由于版本文件不再提供，这里手动生成，还有个 logging，这里就不加了
                "-Djava.library.path=${natives_directory}",
                "-Djna.tmpdir=${natives_directory}",
                "-Dorg.lwjgl.system.SharedLibraryExtractPath=${natives_directory}",
                "-Dio.netty.native.workdir=${natives_directory}",
                "-Dminecraft.launcher.brand=${launcher_name}",
                "-Dminecraft.launcher.version=${launcher_version}",
                // Windows 下的优化，总是 Windows，所以总是添加这一项
                // TODO: 将这一项移动到后期的 Processors 中
                "-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump",
                // 最大内存
                "-Xmx${jvm_max_memory}",
                "-cp",
                "${classpath}"
            ];
            foreach (string arg in jvmArguments)
            {
                builder.AddJvmArgument(arg);
            }

            Logger.LogInformation("Jvm arguments generated, refer to artifact file for details");

            // Java Major Version
            uint firstJreVersion = version.CompatibleJavaMajors?.FirstOrDefault() ?? 8;
            if (!firstJreVersion.Equals(default))
            {
                builder.SetJavaMajorVersion(firstJreVersion);
            }
            else
            {
                throw new BadFormatException("{minecraft_version}", "compatibleJavaMajors");
            }

            Logger.LogInformation("Set java major version compatibility to {major}", firstJreVersion);

            // AssetIndex
            if (version.AssetIndex.HasValue)
            {
                builder.SetAssetIndex(version.AssetIndex.Value.Id, version.AssetIndex.Value.Url,
                    version.AssetIndex.Value.Sha1);
            }
            else
            {
                throw new BadFormatException("{minecraft_version}", "assetIndex");
            }

            Logger.LogInformation("Set asset index to {index}", version.AssetIndex.Value.Id);

            // Main Class Path
            string real = version.MainClass ?? "net.minecraft.client.main.Main";
            builder.SetMainClass(real);

            Logger.LogInformation("Set main class path to {mainClass}", real);

            Context.IsGameInstalled = true;
        }
    }
}