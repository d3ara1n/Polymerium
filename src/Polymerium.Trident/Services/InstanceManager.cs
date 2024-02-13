using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Engines;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Engines.Deploying.Stages;
using Polymerium.Trident.Launching;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services;

public class InstanceManager(IServiceProvider provider, ILogger<InstanceManager> logger)
{
    // 主要是在 UI 线程中增删改查，实际不需要保证线程同步
    private readonly IDictionary<string, TrackerBase> trackers = new ConcurrentDictionary<string, TrackerBase>();

    public event InstanceDeployingHandler? InstanceDeploying;
    public event InstanceLaunchingHandler? InstanceLaunching;

    public DeployTracker Deploy(string key, Metadata metadata)
    {
        if (trackers.ContainsKey(key))
            throw new InvalidOperationException($"The instance is present in the tracking list: {key}");
        var tracker = new DeployTracker(DeployInternalAsync, x => trackers.Remove(x.Key), key, metadata);
        trackers.Add(key, tracker);
        InstanceDeploying?.Invoke(this, new InstanceDeployingEventArgs(key, tracker));
        tracker.Start();
        return tracker;
    }

    private async Task DeployInternalAsync(TrackerBase tracker, CancellationToken token)
    {
        if (tracker is DeployTracker handle)
        {
            logger.LogInformation("Begin launching task for {key}", handle.Key);
            var deployer = provider.GetRequiredService<DeployEngine>();
            deployer.SetProfile(handle.Key, handle.Metadata, handle.Token);
            foreach (var stage in deployer)
                try
                {
                    logger.LogInformation("Enter deployment stage {stage}", stage.GetType().Name);
                    var state = stage switch
                    {
                        CheckArtifactStage => DeployStage.CheckArtifact,
                        InstallVanillaStage => DeployStage.InstallVanilla,
                        ResolveAttachmentStage => DeployStage.ResolveAttachments,
                        ProcessLoaderStage => DeployStage.ProcessLoaders,
                        BuildArtifactStage => DeployStage.BuildArtifact,
                        _ => throw new NotImplementedException()
                    };
                    handle.OnStageUpdate(state);
                    await Task.Delay(1500, token);
                    await stage.ProcessAsync();
                }
                catch (DeployException e)
                {
                    logger.LogError(e, "Deploy failed: {stage}", stage.GetType().Name);
                    throw;
                }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(tracker));
        }
    }

    public LaunchTracker Launch(string key, LaunchOptions options)
    {
        throw new NotImplementedException();
    }

    private async Task LaunchInternalAsync()
    {
        throw new NotImplementedException();
    }

    public bool IsTracking(string key, [MaybeNullWhen(false)] out TrackerBase tracker)
    {
        if (trackers.TryGetValue(key, out tracker))
            return true;

        tracker = null;
        return false;
    }

    public bool IsInUse(string key)
    {
        return trackers.ContainsKey(key);
    }

    public void DeleteInstance(string key)
    {
    }
}