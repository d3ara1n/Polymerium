﻿using Microsoft.Extensions.Logging;
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
            ArtifactBuilder builder = Context.ArtifactBuilder!;
            IEnumerable<Loader> loaders = Context.Metadata.Layers.Where(x => x.Enabled).SelectMany(x => x.Loaders);
            foreach (Loader loader in loaders)
            {
                Logger.LogInformation("Process loader: {id}({version})", loader.Id, loader.Version);
                switch (loader.Id)
                {
                    case Loader.COMPONENT_BUILTIN_STORAGE:
                        Context.ArtifactBuilder!.AddProcessor(TransientData.PROCESSOR_TRIDENT_STORAGE, loader.Version,
                            ConditionOfComponent(Loader.COMPONENT_BUILTIN_STORAGE));
                        break;

                    case Loader.COMPONENT_FORGE:
                        await InstallForgeAsync(builder, loader.Version);
                        break;

                    default:
                        throw new ResourceIdentityUnrecognizedException(loader.Id, nameof(Loader));
                }
            }

            Context.IsLoaderProcessed = true;
        }

        private string ConditionOfComponent(string component)
        {
            return $"component:{component}";
        }

        private async Task InstallForgeAsync(ArtifactBuilder builder, string version)
        {
            PrismVersion index =
                await PrismLauncherHelper.GetVersionAsync(PrismLauncherHelper.UID_FORGE, version, factory,
                    Context.Token);

            PrismLauncherHelper.AddValidatedLibrariesToArtifact(builder,
                index.Libraries ?? Enumerable.Empty<PrismVersionLibrary>());

            foreach (PrismVersionLibrary file in index.MavenFiles ?? Enumerable.Empty<PrismVersionLibrary>())
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

            foreach (string argument in index.MinecraftArguments?.Split(' ') ?? Enumerable.Empty<string>())
            {
                builder.AddGameArgument(argument);
            }

            builder.AddJvmArgument("-Dforgewrapper.librariesDir=${library_directory}");

            Artifact.Library? installer = builder.Libraries.FirstOrDefault(x =>
                x.Id.Platform == "installer" && x.Id.Namespace == "net.minecraftforge" && x.Id.Name == "forge");
            if (installer != null)
            {
                builder.AddJvmArgument(
                    $"-Dforgewrapper.installer={Context.Trident.LibraryPath(installer.Id.Namespace, installer.Id.Name, installer.Id.Version, installer.Id.Platform, installer.Id.Extension)}");
            }

            Artifact.Library? minecraft = builder.Libraries.FirstOrDefault(x =>
                x.Id.Platform == "client" && x.Id.Namespace == "com.mojang" && x.Id.Name == "minecraft");
            if (minecraft != null)
            {
                builder.AddJvmArgument(
                    $"-Dforgewrapper.minecraft={Context.Trident.LibraryPath(minecraft.Id.Namespace, minecraft.Id.Name, minecraft.Id.Version, minecraft.Id.Platform, minecraft.Id.Extension)}");
            }

            // 通过拦截的方式给 ForgeWrapper 注入主要参数，即使没找到也不报错，因为报错需要定义一个异常类型，太麻烦

            builder.SetMainClass(index.MainClass ?? "io.github.zekerzhayard.forgewrapper.installer.Main");
        }
    }
}