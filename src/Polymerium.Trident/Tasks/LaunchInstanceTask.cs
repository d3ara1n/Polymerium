using Microsoft.Extensions.Logging;
using Polymerium.Trident.Engines;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Engines.Deploying.Stages;
using Trident.Abstractions;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Tasks;

public class LaunchInstanceTask(string key, Profile profile, DeployEngine deployer, ILogger<LaunchInstanceTask> logger)
    : TaskBase(key, $"Launch {profile.Name}", "Preparing...")
{
    protected override async Task OnThreadAsync()
    {
        logger.LogInformation("Begin launching task for {key}", key);
        deployer.SetProfile(key, profile.Metadata, Token);
        var stageCount = 9u;
        var stageIndex = 0u;
        foreach (var stage in deployer)
            try
            {
                logger.LogInformation("Enter deployment stage {stage}", stage.GetType().Name);
                var friendlyName = stage switch
                {
                    CheckArtifactStage => "Checking artifact...",
                    InstallVanillaStage => "Installing game...",
                    ResolveAttachmentStage => "Resolving attachments...",
                    ProcessLoaderStage => "Processing loaders...",
                    BuildArtifactStage => "Building artifact...",
                    _ => stage.GetType().Name
                };
                var progress = ++stageIndex * 100 / stageCount;
                ReportProgress(progress > 100 ? 100 : progress, $"Deploy {profile.Name}",
                    friendlyName);
                await stage.ProcessAsync();
            }
            catch (DeployException e)
            {
                logger.LogError(e, "Launch failed while in deployment stage: {stage}", stage.GetType().Name);
                throw;
            }
    }
}