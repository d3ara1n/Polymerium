using Microsoft.Extensions.Logging;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Models.PrismLauncher;
using Trident.Abstractions.Building;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Engines.Deploying.Stages
{
    public class ProcessLoaderStage(IHttpClientFactory factory) : StageBase
    {
        protected override async Task OnProcessAsync()
        {
            var builder = Context.ArtifactBuilder!;
            var loaders = Context.Metadata.Layers.Where(x => x.Enabled).SelectMany(x => x.Loaders);
            foreach (var loader in loaders)
            {
                Logger.LogInformation("Process loader: {id}({version})", loader.Identity, loader.Version);
                switch (loader.Identity)
                {
                    case Loader.COMPONENT_BUILTIN_STORAGE:
                        Context.ArtifactBuilder!.AddProcessor(TransientData.PROCESSOR_TRIDENT_STORAGE, loader.Version,
                            ConditionOfComponent(Loader.COMPONENT_BUILTIN_STORAGE));
                        break;

                    case Loader.COMPONENT_FORGE:
                        await InstallForgeAsync(builder, PrismLauncherHelper.UID_FORGE, loader.Version);
                        break;

                    case Loader.COMPONENT_NEOFORGE:
                        await InstallForgeAsync(builder, PrismLauncherHelper.UID_NEOFORGE, loader.Version);
                        break;

                    case Loader.COMPONENT_FABRIC:
                        await InstallFabricAsync(builder, PrismLauncherHelper.UID_FABRIC, loader.Version);
                        break;

                    case Loader.COMPONENT_QUILT:
                        await InstallFabricAsync(builder, PrismLauncherHelper.UID_QUILT, loader.Version);
                        break;

                    default:
                        throw new ResourceIdentityUnrecognizedException(loader.Identity, nameof(Loader));
                }
            }

            Context.IsLoaderProcessed = true;
        }

        private string ConditionOfComponent(string component)
        {
            return $"component:{component}";
        }

        private async Task InstallForgeAsync(ArtifactBuilder builder, string uid, string version)
        {
            var index =
                await PrismLauncherHelper.GetVersionAsync(uid, version, factory,
                    Context.Token);

            PrismLauncherHelper.AddValidatedLibrariesToArtifact(builder,
                index.Libraries ?? Enumerable.Empty<PrismVersionLibrary>());

            foreach (var file in index.MavenFiles ?? Enumerable.Empty<PrismVersionLibrary>())
            {
                if (file.Downloads.Artifact.HasValue)
                {
                    builder.AddLibrary(file.Name, file.Downloads.Artifact.Value.Url, file.Downloads.Artifact.Value.Sha1,
                        false, false);
                }
            }

            if (index.MinecraftArguments != null && index.MinecraftArguments.Any())
            {
                builder.ClearGameArguments();
            }

            foreach (var argument in index.MinecraftArguments?.Split(' ') ?? Enumerable.Empty<string>())
            {
                builder.AddGameArgument(argument);
            }

            builder.AddJvmArgument("-Dforgewrapper.librariesDir=${library_directory}");

            var installer = builder.Libraries.FirstOrDefault(x =>
                x.Id.Platform == "installer" && x.Id.Namespace == uid && x.Id.Name == "forge");
            if (installer != null)
            {
                builder.AddJvmArgument(
                    $"-Dforgewrapper.installer={Context.Trident.LibraryPath(installer.Id.Namespace, installer.Id.Name, installer.Id.Version, installer.Id.Platform, installer.Id.Extension)}");
            }

            var minecraft = builder.Libraries.FirstOrDefault(x =>
                x.Id.Platform == "client" && x.Id.Namespace == "com.mojang" && x.Id.Name == "minecraft");
            if (minecraft != null)
            {
                builder.AddJvmArgument(
                    $"-Dforgewrapper.minecraft={Context.Trident.LibraryPath(minecraft.Id.Namespace, minecraft.Id.Name, minecraft.Id.Version, minecraft.Id.Platform, minecraft.Id.Extension)}");
            }

            // 通过拦截的方式给 ForgeWrapper 注入主要参数，即使没找到也不报错，因为报错需要定义一个异常类型，太麻烦

            builder.SetMainClass(index.MainClass ?? "io.github.zekerzhayard.forgewrapper.installer.Main");
        }

        private async Task InstallFabricAsync(ArtifactBuilder builder, string uid, string version)
        {
            var index =
                await PrismLauncherHelper.GetVersionAsync(uid, version, factory,
                    Context.Token);

            PrismLauncherHelper.AddValidatedLibrariesToArtifact(builder,
                index.Libraries ?? Enumerable.Empty<PrismVersionLibrary>());

            var intermediary = await PrismLauncherHelper.GetVersionAsync(PrismLauncherHelper.UID_INTERMEDIARY, Context.Metadata.Version, factory, Context.Token);

            PrismLauncherHelper.AddValidatedLibrariesToArtifact(builder,
                intermediary.Libraries ?? Enumerable.Empty<PrismVersionLibrary>());

            builder.SetMainClass(index.MainClass ?? "net.fabricmc.loader.impl.launch.knot.KnotClient");
        }
    }
}