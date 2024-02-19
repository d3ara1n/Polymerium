using Microsoft.Extensions.Logging;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Models.PrismLauncher.Minecraft;
using System.Runtime.InteropServices;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Extensions;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class InstallVanillaStage(IHttpClientFactory factory) : StageBase
{
    protected override async Task OnProcessAsync()
    {
        var builder = Context.ArtifactBuilder!;
        using var client = factory.CreateClient();
        var manifest = await MinecraftHelper.GetManifestAsync(factory, Context.Token);
        Logger.LogInformation("Got manifest with {count} entries", manifest.Versions.Length);
        var index = manifest.Versions.FirstOrDefault(x => x.Version == Context.Metadata.Version);
        if (index.Equals(default))
            throw new BadFormatException("{minecraft_manifest}", $"versions[version:{Context.Metadata.Version}]");
        var version = await MinecraftHelper.GetVersionAsync(index.Version, factory, Context.Token);
        Logger.LogInformation("Got version index {version}({uid})", version.Version, version.Uid);

        // Libraries
        var patched = await MinecraftHelper.GetPrismPatchedLibraries(version, factory, Context.Token);
        var osString = PlatformHelper.GetOsName();
        var osArchString = $"{osString}-{PlatformHelper.GetOsArch()}";
        Logger.LogInformation("Current os string: {string}", osArchString);
        var libraries = patched.Where(x =>
        {
            if (x.Rules != null && x.Rules.Any())
                return x.Rules.Any(y =>
                {
                    var pass = true;
                    if (y.Os != null && y.Os.TryGetValue("name", out var os))
                        // name
                        pass = osArchString == os || osString == os;

                    return y.Action == PrismMinecraftVersionLibraryRuleAction.Allow ? pass : !pass;
                });

            return true;
        }) ?? Enumerable.Empty<PrismMinecraftVersionLibrary>();
        foreach (var lib in libraries)
        {
            if (lib.Downloads.Artifact.HasValue)
                builder.AddLibrary(lib.Name, lib.Downloads.Artifact.Value.Url, lib.Downloads.Artifact.Value.Sha1);

            if (lib.Natives.HasValue && lib.Natives.Value.Windows != null)
            {
                var classifier = lib.Natives.Value.Windows.Replace(
                    "${arch}",
                    Environment.Is64BitOperatingSystem ? "64" : "32"
                );
                if (lib.Downloads.Classifiers.TryGetValue(classifier, out var download))
                    builder.AddLibrary(lib.Name, download.Url, download.Sha1, true, false);
            }
        }

        Logger.LogInformation("Libraries added, refer to artifact file for details");

        // Main Jar as a Library as well
        if (version.MainJar != null && version.MainJar.Value.Downloads.Artifact.HasValue)
            builder.AddLibrary(version.MainJar.Value.Name, version.MainJar.Value.Downloads.Artifact.Value.Url,
                version.MainJar.Value.Downloads.Artifact.Value.Sha1);
        else
            throw new BadFormatException("{minecraft_version}", "mainJar.downloads.artifact");

        Logger.LogInformation("Client jar appended: {name}", version.MainJar.Value.Name);

        // Game Arguments
        var arguments = (version.MinecraftArguments?.Split(' ') ?? Enumerable.Empty<string>()).Concat([
            // 额外添加的窗口设定
            "--width",
            "${resolution_width}",
            "--height",
            "${resolution_height}"
        ]);
        foreach (var arg in arguments)
            builder.AddGameArgument(arg);

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
        foreach (var arg in jvmArguments)
            builder.AddJvmArgument(arg);

        Logger.LogInformation("Jvm arguments generated, refer to artifact file for details");

        // Java Major Version
        var firstJreVersion = version.CompatibleJavaMajors?.FirstOrDefault() ?? 8;
        if (!firstJreVersion.Equals(default))
            builder.SetJavaMajorVersion(firstJreVersion);
        else
            throw new BadFormatException("{minecraft_version}", "compatibleJavaMajors");

        Logger.LogInformation("Set java major version compatibility to {major}", firstJreVersion);

        // AssetIndex
        if (version.AssetIndex.HasValue)
            builder.SetAssetIndex(version.AssetIndex.Value.Id, version.AssetIndex.Value.Url, version.AssetIndex.Value.Sha1);
        else
            throw new BadFormatException("{minecraft_version}", "assetIndex");

        Logger.LogInformation("Set asset index to {index}", version.AssetIndex.Value.Id);

        // Main Class Path
        var real = version.MainClass ?? "net.minecraft.client.main.Main";
        builder.SetMainClass(real);

        Logger.LogInformation("Set main class path to {mainClass}", real);

        Context.IsGameInstalled = true;
    }


}