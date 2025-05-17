using Microsoft.Extensions.Logging;
using Polymerium.Trident.Models.PrismLauncherApi;
using Polymerium.Trident.Services;
using Trident.Abstractions;
using Trident.Abstractions.Utilities;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class ProcessLoaderStage(
    ILogger<ProcessLoaderStage> logger,
    PrismLauncherService prismLauncherService) : StageBase
{
    protected override async Task OnProcessAsync(CancellationToken token)
    {
        var loader = Context.Setup.Loader;
        var builder = Context.ArtifactBuilder!;
        logger.LogInformation("Process loader: {}", loader ?? "(None)");
        if (loader != null)
            if (LoaderHelper.TryParse(loader, out var parsed))
                switch (parsed.Identity)
                {
                    case LoaderHelper.LOADERID_FORGE:
                        await InstallForgeAsync(builder, PrismLauncherService.UID_FORGE, parsed.Version, token)
                           .ConfigureAwait(false);
                        break;

                    case LoaderHelper.LOADERID_NEOFORGE:
                        await InstallForgeAsync(builder, PrismLauncherService.UID_NEOFORGE, parsed.Version, token)
                           .ConfigureAwait(false);
                        break;

                    case LoaderHelper.LOADERID_FABRIC:
                        await InstallFabricAsync(builder, PrismLauncherService.UID_FABRIC, parsed.Version, token)
                           .ConfigureAwait(false);
                        break;

                    case LoaderHelper.LOADERID_QUILT:
                        await InstallFabricAsync(builder, PrismLauncherService.UID_QUILT, parsed.Version, token)
                           .ConfigureAwait(false);
                        break;

                    default:
                        throw new FormatException($"{parsed.Identity} is not known loader");
                }

        Context.IsLoaderProcess = true;
    }

    private async Task InstallForgeAsync(DataLockBuilder builder, string uid, string version, CancellationToken token)
    {
        var index = await prismLauncherService.GetVersionAsync(uid, version, token).ConfigureAwait(false);

        PrismLauncherService.AddValidatedLibrariesToArtifact(builder,
                                                             index.Libraries ?? Enumerable.Empty<Component.Library>());

        foreach (var file in index.MavenFiles ?? Enumerable.Empty<Component.Library>())
            if (file.Downloads?.Artifact != null)
                builder.AddLibrary(file.Name, file.Downloads.Artifact.Url, file.Downloads.Artifact.Sha1, false, false);

        if (index.MinecraftArguments != null && index.MinecraftArguments.Any())
            builder.ClearGameArguments();

        foreach (var argument in index.MinecraftArguments?.Split(' ') ?? Enumerable.Empty<string>())
            builder.AddGameArgument(argument);

        if (index.Tweakers != null)
            // 不知道如果列表有多个元素该如何添加，估计不会有多个，这是一个大胆的估计！
            foreach (var tweaker in index.Tweakers)
            {
                builder.AddGameArgument("--tweakClass");
                builder.AddGameArgument(tweaker);
            }

        builder.AddJvmArgument("-Dforgewrapper.librariesDir=${library_directory}");

        var installer = builder.Libraries.FirstOrDefault(x => x.Id.Platform == "installer"
                                                           && x.Id.Namespace == uid
                                                           && x.Id.Name == "forge");
        if (installer != null)
            builder.AddJvmArgument($"-Dforgewrapper.installer={PathDef.Default.FileOfLibrary(installer.Id.Namespace, installer.Id.Name, installer.Id.Version, installer.Id.Platform, installer.Id.Extension)}");

        var minecraft = builder.Libraries.FirstOrDefault(x => x.Id is
        {
            Platform: "client",
            Namespace: "com.mojang",
            Name: "minecraft"
        });
        if (minecraft != null)
            builder.AddJvmArgument($"-Dforgewrapper.minecraft={PathDef.Default.FileOfLibrary(minecraft.Id.Namespace, minecraft.Id.Name, minecraft.Id.Version, minecraft.Id.Platform, minecraft.Id.Extension)}");

        // 通过拦截的方式给 ForgeWrapper 注入主要参数，即使没找到也不报错，因为报错需要定义一个异常类型，太麻烦

        builder.SetMainClass(index.MainClass ?? "io.github.zekerzhayard.forgewrapper.installer.Main");
    }

    private async Task InstallFabricAsync(DataLockBuilder builder, string uid, string version, CancellationToken token)
    {
        var index = await prismLauncherService.GetVersionAsync(uid, version, token).ConfigureAwait(false);

        PrismLauncherService.AddValidatedLibrariesToArtifact(builder,
                                                             index.Libraries ?? Enumerable.Empty<Component.Library>());

        var intermediary = await prismLauncherService
                                .GetVersionAsync(PrismLauncherService.UID_INTERMEDIARY, Context.Setup.Version, token)
                                .ConfigureAwait(false);

        PrismLauncherService.AddValidatedLibrariesToArtifact(builder,
                                                             intermediary.Libraries
                                                          ?? Enumerable.Empty<Component.Library>());

        builder.SetMainClass(index.MainClass ?? "net.fabricmc.loader.impl.launch.knot.KnotClient");
    }
}