using Microsoft.Extensions.Logging;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class ProcessLoaderStage : StageBase
{
    protected override async Task OnProcessAsync()
    {
        var loaders = Context.Metadata.Layers.Where(x => x.Enabled).SelectMany(x => x.Loaders);
        foreach (var loader in loaders)
        {
            Logger.LogInformation("Process loader: {id}({version})", loader.Id, loader.Version);
            switch (loader.Id)
            {
                case Loader.COMPONENT_BUILTIN_STORAGE:
                    Context.ArtifactBuilder!.AddProcessor(TransientData.PROCESSOR_TRIDENT_STORAGE, loader.Version,
                        $"component:{Loader.COMPONENT_BUILTIN_STORAGE}");
                    break;

                case Loader.COMPONENT_FORGE:
                    // TODO:
                    break;

                default:
                    throw new ResourceIdentityUnrecognizedException(loader.Id, nameof(Loader));
            }
        }

        Context.IsLoaderProcessed = true;
    }
}