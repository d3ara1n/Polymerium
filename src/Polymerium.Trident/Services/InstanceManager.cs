using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Engines;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Engines.Deploying.Stages;
using Polymerium.Trident.Engines.Launching;
using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Launching;
using Polymerium.Trident.Services.Instances;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Trident.Abstractions;
using Trident.Abstractions.Building;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services
{
    public class InstanceManager(
        IServiceProvider provider,
        ILogger<InstanceManager> logger,
        TridentContext trident,
        JsonSerializerOptions serializerOptions)
    {
        // 主要是在 UI 线程中增删改查，实际不需要保证线程同步
        private readonly IDictionary<string, TrackerBase> trackers = new ConcurrentDictionary<string, TrackerBase>();

        public event InstanceDeployingHandler? InstanceDeploying;
        public event InstanceLaunchingHandler? InstanceLaunching;

        public DeployTracker Deploy(string key, Profile profile, ICollection<string>? keywords = null,
            Action? onSuccess = null,
            CancellationToken cancellationToken = default)
        {
            if (IsInUse(key))
            {
                throw new InvalidOperationException($"The instance is present in the tracking list: {key}");
            }

            DeployTracker tracker = new(
                x => DeployInternalAsync(x, profile, keywords ?? new List<string>()),
                x =>
                {
                    trackers.Remove(x.Key);
                    if (x.State == TaskState.Finished)
                    {
                        onSuccess?.Invoke();
                    }
                }, key, profile.Metadata, cancellationToken);
            trackers.Add(key, tracker);
            InstanceDeploying?.Invoke(this, new InstanceDeployingEventArgs(key, tracker));
            tracker.Start();
            return tracker;
        }

        public LaunchTracker Launch(string key, Profile profile, LaunchOptions options, Action? onSuccess = null,
            CancellationToken cancellationToken = default)
        {
            if (IsInUse(key))
            {
                throw new InvalidOperationException($"The instance is present in the tracking list: {key}");
            }

            LaunchTracker tracker = new(key, async x => await LaunchInternalAsync(x, profile, options), x =>
            {
                trackers.Remove(x.Key);
                if (x.State == TaskState.Finished)
                {
                    onSuccess?.Invoke();
                }
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
                DateTimeOffset beginTime = DateTimeOffset.Now;
                DeployEngine deployer = provider.GetRequiredService<DeployEngine>();
                deployer.SetProfile(handle.Key, handle.Metadata, keywords, handle.Token);
                foreach (StageBase stage in deployer)
                {
                    try
                    {
                        logger.LogInformation("Enter deployment stage {stage}", stage.GetType().Name);
                        DeployStage state = stage switch
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
                        {
                            solidify.SetHandler(handle.OnFileSolidified);
                        }

                        handle.OnStageUpdate(state);
                        await stage.ProcessAsync();
                        await Task.Delay(1000, handle.Token);
                    }
                    catch (DeployException e)
                    {
                        logger.LogError(e, "Deploy failed: {stage}", stage.GetType().Name);
                        profile.Records.Timeline.Add(new Profile.RecordData.TimelinePoint(false, profile.Reference,
                            Profile.RecordData.TimelinePoint.TimelimeAction.Deploy, beginTime, DateTimeOffset.Now));
                        throw;
                    }
                }

                logger.LogInformation("Deploy {key} finished", handle.Key);
                profile.Records.Timeline.Add(new Profile.RecordData.TimelinePoint(true, profile.Reference,
                    Profile.RecordData.TimelinePoint.TimelimeAction.Deploy, beginTime, DateTimeOffset.Now));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(tracker));
            }
        }

        private async Task LaunchInternalAsync(TrackerBase tracker, Profile profile, LaunchOptions options)
        {
            if (tracker is LaunchTracker handle)
            {
                logger.LogInformation("Begin launch task for {key}", handle.Key);
                DateTimeOffset beginTime = DateTimeOffset.Now;
                string artifactPath = trident.InstanceArtifactPath(tracker.Key);
                bool found = false;
                if (File.Exists(artifactPath))
                {
                    found = true;
                    string content = await File.ReadAllTextAsync(artifactPath);
                    Artifact? artifact = JsonSerializer.Deserialize<Artifact>(content, serializerOptions);
                    if (artifact != null &&
                        artifact.Verify(tracker.Key, profile.Metadata.ComputeWatermark(), trident.HomeDir))
                    {
                        uint jreVersion = artifact.JavaMajorVersion;
                        try
                        {
                            string jreExecutable = Path.Combine(options.JavaHomeLocator.Invoke(jreVersion), "bin",
                                "javaw.exe");
                            string working_dir = trident.InstanceHomePath(tracker.Key);
                            string library_dir = trident.LibraryDir;
                            string asset_dir = trident.AssetDir;
                            string native_dir = trident.NativeDirPath(tracker.Key);
                            Igniter igniter = artifact.MakeIgniter(trident);
                            igniter
                                .AddGameArgument("--width")
                                .AddGameArgument("${resolution_width}")
                                .AddGameArgument("--height")
                                .AddGameArgument("${resolution_height}");
                            igniter
                                .SetJreExecutable(jreExecutable)
                                .SetWorkingDirectory(working_dir)
                                .SetAssetRootDirectory(asset_dir)
                                .SetNativetRootDirectory(native_dir)
                                .SetLibraryRootDirectory(library_dir)
                                .SetClassPathSeparator(';')
                                .SetLauncherName("Polymerium")
                                .SetLauncherVersion(Assembly.GetExecutingAssembly().GetName().Version?.ToString() ??
                                                    "Eternal")
                                .SetOsName(PlatformHelper.GetOsName())
                                .SetOsArch(PlatformHelper.GetOsArch())
                                .SetOsVersion(PlatformHelper.GetOsVersion())
                                .SetUserUuid("178804A6-08D0-4434-AD84-40DD7E9273F1")
                                .SetUserType("legacy")
                                .SetUserName("Steve")
                                .SetUserAccessToken("invalid")
                                .SetVersionName(profile.Metadata.Version)
                                .SetWindowSize(options.WindowSize)
                                .SetMaxMemory(options.MaxMemory)
                                .SetReleaseType("Polyermium");
                            foreach (string additional in options.AdditionalArguments.Split(' '))
                            {
                                igniter.AddJvmArgument(additional);
                            }

                            Process process = igniter.Build();
                            await File.WriteAllLinesAsync(
                                Path.Combine(trident.InstanceHomePath(tracker.Key), "dump.txt"),
                                process.StartInfo.ArgumentList);
                            handle.OnFired();
                            if (options.Mode == LaunchMode.Managed)
                            {
                                LaunchEngine launcher = provider.GetRequiredService<LaunchEngine>();
                                process.Start();
                                await foreach (Scrap scrap in launcher.WithCancellation(handle.Token)
                                                   .ConfigureAwait(false))
                                {
                                }
                            }
                            else
                            {
                                process.Start();
                            }

                            profile.Records.Timeline.Add(new Profile.RecordData.TimelinePoint(true, profile.Reference,
                                Profile.RecordData.TimelinePoint.TimelimeAction.Play, beginTime, DateTimeOffset.Now));
                            return;
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, "Compile launch arguments failed due to exception: {ex}", e.Message);
                            profile.Records.Timeline.Add(new Profile.RecordData.TimelinePoint(false, profile.Reference,
                                Profile.RecordData.TimelinePoint.TimelimeAction.Play, beginTime, DateTimeOffset.Now));
                            throw new LaunchException(tracker.Key, e);
                        }
                    }
                }


                ArtifactUnavailableException innerException = new(tracker.Key, artifactPath, found);
                logger.LogError(innerException, $"Artifact of {tracker.Key} not available");
                profile.Records.Timeline.Add(new Profile.RecordData.TimelinePoint(false, profile.Reference,
                    Profile.RecordData.TimelinePoint.TimelimeAction.Play, beginTime, DateTimeOffset.Now));
                throw new LaunchException(tracker.Key, innerException);
            }

            throw new ArgumentOutOfRangeException(nameof(tracker));
        }

        private void UpdateInternal()
        {
            throw new NotImplementedException();
        }

        public bool IsTracking(string key, [MaybeNullWhen(false)] out TrackerBase tracker)
        {
            if (trackers.TryGetValue(key, out tracker))
            {
                return true;
            }

            tracker = null;
            return false;
        }

        public bool IsInUse(string key)
        {
            return trackers.ContainsKey(key);
        }
    }
}