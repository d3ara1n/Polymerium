using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Engines;
using Polymerium.Trident.Engines.Deploying.Stages;
using Polymerium.Trident.Engines.Launching;
using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Launching;
using Polymerium.Trident.Services.Instances;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Trident.Abstractions;
using Trident.Abstractions.Building;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services;

public class InstanceManager(IServiceProvider provider, ILogger<InstanceManager> logger, TridentContext trident, JsonSerializerOptions serializerOptions)
{
    // 主要是在 UI 线程中增删改查，实际不需要保证线程同步
    private readonly IDictionary<string, TrackerBase> trackers = new ConcurrentDictionary<string, TrackerBase>();

    public event InstanceDeployingHandler? InstanceDeploying;
    public event InstanceLaunchingHandler? InstanceLaunching;

    public DeployTracker Deploy(string key, Profile profile, ICollection<string>? keywords = null, Action? onSuccess = null,
        CancellationToken cancellationToken = default)
    {
        if (IsInUse(key))
            throw new InvalidOperationException($"The instance is present in the tracking list: {key}");
        var tracker = new DeployTracker(
            x => DeployInternalAsync(x, profile, keywords ?? new List<string>()),
            x =>
            {
                trackers.Remove(x.Key);
                if (x.State == TaskState.Finished)
                    onSuccess?.Invoke();
            }, key, profile.Metadata, cancellationToken);
        trackers.Add(key, tracker);
        InstanceDeploying?.Invoke(this, new InstanceDeployingEventArgs(key, tracker));
        tracker.Start();
        return tracker;
    }

    public LaunchTracker Launch(string key, Profile profile, LaunchOptions options, Action? onSuccess = null, CancellationToken cancellationToken = default)
    {
        if (IsInUse(key))
            throw new InvalidOperationException($"The instance is present in the tracking list: {key}");
        var tracker = new LaunchTracker(key, async (x) => await LaunchInternalAsync(x, profile.Metadata, options), (x) =>
        {
            trackers.Remove(x.Key);
            if (x.State == TaskState.Finished)
                onSuccess?.Invoke();
        }, cancellationToken);
        trackers.Add(key, tracker);
        InstanceLaunching?.Invoke(this, new InstanceLaunchingEventArgs(key, tracker, options.Mode));
        tracker.Start();
        return tracker;
    }

    public UpdateTracker Update()
    {
        throw new NotImplementedException();
    }

    private async Task DeployInternalAsync(TrackerBase tracker, Profile profile, ICollection<string> keywords)
    {
        if (tracker is DeployTracker handle)
        {
            logger.LogInformation("Begin deploy task for {key}", handle.Key);
            var beginTime = DateTimeOffset.Now;
            var deployer = provider.GetRequiredService<DeployEngine>();
            deployer.SetProfile(handle.Key, handle.Metadata, keywords, handle.Token);
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
                        BuildTransientStage => DeployStage.BuildTransient,
                        SolidifyTransientStage => DeployStage.SolidifyTransient,
                        _ => throw new NotImplementedException()
                    };
                    if (stage is SolidifyTransientStage solidify)
                        solidify.SetHandler(handle.OnFileSolidified);

                    handle.OnStageUpdate(state);
                    await stage.ProcessAsync();
                    await Task.Delay(1000, handle.Token);
                }
                catch (DeployException e)
                {
                    logger.LogError(e, "Deploy failed: {stage}", stage.GetType().Name);
                    profile.Records.Timeline.Add(new Profile.RecordData.TimelinePoint(false, profile.Reference, Profile.RecordData.TimelinePoint.TimelimeAction.Deploy, beginTime, DateTimeOffset.Now));
                    throw;
                }
            logger.LogInformation("Deploy {key} finished", handle.Key);
            profile.Records.Timeline.Add(new Profile.RecordData.TimelinePoint(true, profile.Reference, Profile.RecordData.TimelinePoint.TimelimeAction.Deploy, beginTime, DateTimeOffset.Now));
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(tracker));
        }
    }

    private async Task LaunchInternalAsync(TrackerBase tracker, Metadata metadata, LaunchOptions options)
    {
        if (tracker is LaunchTracker handle)
        {
            logger.LogInformation("Begin launch task for {key}", handle.Key);
            // build material

            var artifactPath = trident.InstanceArtifactPath(tracker.Key);
            bool found = false;
            if (File.Exists(artifactPath))
            {
                found = true;
                var content = await File.ReadAllTextAsync(artifactPath);
                var artifact = JsonSerializer.Deserialize<Artifact>(content, serializerOptions);
                if (artifact != null && artifact.Verify(tracker.Key, metadata.ComputeWatermark(), trident.HomeDir))
                {
                    var jreVersion = artifact.JavaMajorVersion;
                    try
                    {
                        // FIXME: replace java.exe with javaw.exe
                        var jreExecutable = Path.Combine(options.JavaHomeLocator.Invoke(jreVersion), "bin", "java.exe");
                        var working_dir = trident.InstanceHomePath(tracker.Key);
                        var library_dir = trident.LibraryDir;
                        var asset_dir = trident.AssetDir;
                        var native_dir = trident.NativeDirPath(tracker.Key);
                        var igniter = artifact.MakeIgniter(trident);
                        igniter
                            .SetJreExecutable(jreExecutable)
                            .SetWorkingDirectory(working_dir)
                            .SetAssetRootDirectory(asset_dir)
                            .SetNativetRootDirectory(native_dir)
                            .SetClassPathSeparator(';')
                            .SetLauncherName("Polymerium")
                            .SetLauncherVersion(Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Eternal")
                            .SetOsName(PlatformHelper.GetOsName())
                            .SetOsArch(PlatformHelper.GetOsArch())
                            .SetOsVersion(PlatformHelper.GetOsVersion())
                            .SetUserUuid("178804A6-08D0-4434-AD84-40DD7E9273F1")
                            .SetUserType("legacy")
                            .SetUserName("Steve")
                            .SetUserAccessToken("invalid")
                            .SetVersionName(metadata.Version)
                            .SetWindowSize(options.WindowSize)
                            .SetMaxMemory(options.MaxMemory)
                            .SetReleaseType("Polyermium");
                        foreach (var additional in options.AdditionalArguments.Split(' '))
                            igniter.AddJvmArgument(additional);

                        var process = igniter.Build();
                        await File.WriteAllLinesAsync(Path.Combine(trident.InstanceHomePath(tracker.Key), "dump.txt"), process.StartInfo.ArgumentList);
                        process.Start();
                        // call only in Managed
                        // handle.OnLaunched(process);
                        // TODO: Redirect & collect launcher's output(only in Managed mode)
                        // wait process until die
                        // var launcher = provider.GetRequiredService<LaunchEngine>();
                        return;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Compile launch arguments failed due to exception: {ex}", e.Message);
                        throw new LaunchException(tracker.Key, e);
                    }
                }
            }

            var innerException = new ArtifactUnavailableException(tracker.Key, artifactPath, found);
            logger.LogError(innerException, $"Artifact of {tracker.Key} not available");
            throw new LaunchException(tracker.Key, innerException);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(tracker));
        }
    }

    private void UpdateInternal()
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
}