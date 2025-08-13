using Microsoft.Extensions.Logging;
using Polymerium.Trident.Services;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class InstallVanillaStage(
    ILogger<InstallVanillaStage> logger,
    PrismLauncherService prismLauncherService) : StageBase
{
    protected override async Task OnProcessAsync(CancellationToken token)
    {
        var builder = Context.ArtifactBuilder!;

        var version = await prismLauncherService
                           .GetVersionAsync(PrismLauncherService.UID_MINECRAFT, Context.Setup.Version, token)
                           .ConfigureAwait(false);
        logger.LogInformation("Got version index {version}({uid})", version.Version, version.Uid);

        // Libraries
        var patched = await prismLauncherService.GetPatchedLibraries(version, token).ConfigureAwait(false);
        PrismLauncherService.AddValidatedLibrariesToArtifact(builder, patched);

        logger.LogInformation("Libraries added, refer to artifact file for details");

        // Main Jar as a Library as well
        if (version.MainJar is { Name: { } name, Downloads.Artifact: { } artifact })
        {
            builder.AddLibrary(name, artifact.Url, artifact.Sha1);
            logger.LogInformation("Client jar appended: {name}", name);
        }
        else
        {
            throw new FormatException("{minecraft_version}/mainJar.downloads.artifact");
        }

        // Game Arguments
        var arguments = version.MinecraftArguments?.Split(' ') ?? Enumerable.Empty<string>();
        foreach (var arg in arguments)
            builder.AddGameArgument(arg);

        logger.LogInformation("Game arguments added, refer to artifact file for details");

        // Jvm Arguments
        string[] jvmArguments =
        [
            // 由于版本文件不再提供，这里手动生成，还有个 logging，这里就不加了
            "-Djava.library.path=${natives_directory}",
            "-DlibraryDirectory=${library_directory}",
            "-Djna.tmpdir=${natives_directory}",
            "-Dorg.lwjgl.system.SharedLibraryExtractPath=${natives_directory}",
            "-Dio.netty.native.workdir=${natives_directory}",
            "-Dminecraft.launcher.brand=${launcher_name}",
            "-Dminecraft.launcher.version=${launcher_version}",
            // TODO: Windows 专供，不知道放 Linux 和 MaxOS 会不会出错
            "-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump",
            // 最大内存
            "-Xmx${jvm_max_memory}",
            "-cp",
            "${classpath}"
        ];
        foreach (var arg in jvmArguments)
            builder.AddJvmArgument(arg);

        logger.LogInformation("Jvm arguments generated, refer to artifact file for details");

        // Java Major Version
        var firstJreVersion = version.CompatibleJavaMajors?.FirstOrDefault() ?? 8u;
        if (!firstJreVersion.Equals(0))
            builder.SetJavaMajorVersion(firstJreVersion);
        else
            throw new FormatException("{minecraft_version}/compatibleJavaMajors");

        logger.LogInformation("Set java major version compatibility to {major}", firstJreVersion);

        // AssetIndex
        if (version.AssetIndex is { } index)
        {
            builder.SetAssetIndex(index.Id, index.Url, index.Sha1);
            logger.LogInformation("Set asset index to {index}", index.Id);
        }
        else
        {
            throw new FormatException("{minecraft_version}/assetIndex");
        }


        // Main Class Path
        var real = version.MainClass ?? "net.minecraft.client.main.Main";
        builder.SetMainClass(real);

        logger.LogInformation("Set main class path to {mainClass}", real);

        Context.IsVanillaInstalled = true;
    }
}