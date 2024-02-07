using Microsoft.Extensions.Logging;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class ProcessLoaderStage : StageBase
{
    protected override async Task OnProcessAsync()
    {
        var loaders = Context.Metadata.Layers.Where(x => x.Enabled).SelectMany(x => x.Loaders);
        foreach (var loader in loaders)
        {
            // TODO:
        }

        Logger.LogInformation("None of loaders processed, refer to artifact file for details");
        Context.IsLoaderProcessed = true;
    }
}