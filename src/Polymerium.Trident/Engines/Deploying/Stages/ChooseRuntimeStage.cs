using Polymerium.Trident.Services;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class ChooseRuntimeStage(PrismLauncherService prismLauncherService) : StageBase
{
    protected override async Task OnProcessAsync(CancellationToken token)
    {
        if (Context.Options.JavaHomeOverride != null)
            return;

        var major = Context.Artifact!.JavaMajorVersion;

        var manifest = await prismLauncherService.GetRuntimeAsync(major, token);
        
    }
}