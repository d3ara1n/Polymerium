using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Engines;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Engines.Deploying.Stages;
using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Igniters;
using Polymerium.Trident.Services.Instances;
using Polymerium.Trident.Services.Profiles;
using Polymerium.Trident.Utilities;
using Trident.Abstractions;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Importers;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Tasks;
using Trident.Abstractions.Utilities;

namespace Polymerium.Trident.Services;

public class InstanceManager(
    ILogger<InstanceManager> logger,
    ProfileManager profileManager,
    RepositoryAgent repositories,
    ImporterAgent importers,
    IServiceProvider provider,
    IHttpClientFactory clientFactory)
{
    // 主要是在 UI 线程中增删改查，实际不需要保证线程同步
    private readonly Dictionary<string, TrackerBase> _trackers = new();
    public event EventHandler<InstallTracker>? InstanceInstalling;
    public event EventHandler<UpdateTracker>? InstanceUpdating;
    public event EventHandler<DeployTracker>? InstanceDeploying;
    public event EventHandler<LaunchTracker>? InstanceLaunching;

    private void TrackerOnCompleted(TrackerBase tracker)
    {
        tracker.Dispose();
        _trackers.Remove(tracker.Key);
    }

    public bool IsTracking(string key, [MaybeNullWhen(false)] out TrackerBase tracker)
    {
        if (_trackers.TryGetValue(key, out var value))
        {
            tracker = value;
            return true;
        }

        tracker = null;
        return false;
    }

    public bool IsInUse(string key) => _trackers.ContainsKey(key);

    public void DeployAndLaunch(string key, LaunchOptions options)
    {
        if (IsInUse(key))
            throw new InvalidOperationException($"Instance {key} is operated in progress");

        var tracker = new DeployTracker(key,
                                        async t => await DeployInternalAsync((DeployTracker)t),
                                        t =>
                                        {
                                            TrackerOnCompleted(t);
                                            if (t is { State: TrackerState.Finished })
                                                Launch(key, options);
                                        });
        _trackers.Add(key, tracker);
        InstanceDeploying?.Invoke(this, tracker);
        tracker.Start();
    }

    #region Common

    private static async Task<MemoryStream> DownloadFileAsync(
        Uri download,
        ulong size,
        Subject<double?>? reporter,
        HttpClient client,
        CancellationToken token)
    {
        var stream = await client.GetStreamAsync(download, token);
        var memory = new MemoryStream();
        var buffer = new byte[8 * 1024];
        int read;
        var totalRead = 0L;
        do
        {
            read = await stream.ReadAsync(buffer, token);
            await memory.WriteAsync(buffer.AsMemory(0, read), token);
            totalRead += read;
            var progress = (double)(totalRead * 100) / size;
            reporter?.OnNext(progress);
        } while (!token.IsCancellationRequested && read > 0);

        stream.Close();

        memory.Position = 0;
        return memory;
    }

    private static async Task ExtractIconFileAsync(string key, ImportedProfileContainer container, HttpClient client)
    {
        var iconReader = await client.GetStreamAsync(container.IconUrl);
        var iconMemory = new MemoryStream();
        await iconReader.CopyToAsync(iconMemory);
        iconMemory.Position = 0;
        var extension = FileHelper.GuessBitmapExtension(iconMemory);
        var iconPath = PathDef.Default.FileOfIcon(key, extension);
        var dir = Path.GetDirectoryName(iconPath);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        iconMemory.Position = 0;
        var iconWriter = new FileStream(iconPath, FileMode.Create);
        await iconMemory.CopyToAsync(iconWriter);
        await iconWriter.FlushAsync();
        iconWriter.Close();
        iconMemory.Close();
        iconReader.Close();
    }

    #endregion

    #region Deploy

    public DeployTracker Deploy(string key)
    {
        if (IsInUse(key))
            throw new InvalidOperationException($"Instance {key} is operated in progress");

        var tracker = new DeployTracker(key,
                                        async t => await DeployInternalAsync((DeployTracker)t),
                                        TrackerOnCompleted);
        _trackers.Add(key, tracker);
        InstanceDeploying?.Invoke(this, tracker);
        tracker.Start();
        return tracker;
    }

    private async Task DeployInternalAsync(DeployTracker tracker)
    {
        logger.LogInformation("Begin deploy {}", tracker.Key);

        var profile = profileManager.GetImmutable(tracker.Key);
        var engine = new DeployEngine(tracker.Key, profile.Setup, provider, new DeployEngineOptions());

        var watch = Stopwatch.StartNew();
        foreach (var stage in engine)
        {
            if (tracker.Token.IsCancellationRequested)
                break;
            switch (stage)
            {
                case CheckArtifactStage:
                    tracker.StageStream.OnNext(DeployStage.CheckArtifact);
                    tracker.CurrentStage = DeployStage.CheckArtifact;
                    break;
                case InstallVanillaStage:
                    tracker.StageStream.OnNext(DeployStage.InstallVanilla);
                    tracker.CurrentStage = DeployStage.InstallVanilla;
                    break;
                case ProcessLoaderStage:
                    tracker.StageStream.OnNext(DeployStage.ProcessLoader);
                    tracker.CurrentStage = DeployStage.ProcessLoader;
                    break;
                case ResolvePackageStage resolvePackageStage:
                    tracker.StageStream.OnNext(DeployStage.ResolvePackage);
                    tracker.CurrentStage = DeployStage.ResolvePackage;
                    resolvePackageStage
                       .ProgressStream.Subscribe(tracker.ProgressStream)
                       .DisposeWith(resolvePackageStage);
                    break;
                case BuildArtifactStage:
                    tracker.StageStream.OnNext(DeployStage.BuildArtifact);
                    tracker.CurrentStage = DeployStage.BuildArtifact;
                    break;
                case GenerateManifestStage:
                    tracker.StageStream.OnNext(DeployStage.GenerateManifest);
                    tracker.CurrentStage = DeployStage.GenerateManifest;
                    break;
                case SolidifyManifestStage solidifyManifestStage:
                    tracker.StageStream.OnNext(DeployStage.SolidifyManifest);
                    tracker.CurrentStage = DeployStage.SolidifyManifest;
                    solidifyManifestStage
                       .ProgressStream.Subscribe(tracker.ProgressStream)
                       .DisposeWith(solidifyManifestStage);
                    break;
            }

            logger.LogInformation("Enter stage {}", stage.GetType().Name);
            await stage.ProcessAsync(tracker.Token);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        watch.Stop();
        logger.LogInformation("{} deployed in {}ms", tracker.Key, watch.ElapsedMilliseconds);
    }

    #endregion

    #region Launch

    public LaunchTracker Launch(string key, LaunchOptions options)
    {
        if (IsInUse(key))
            throw new InvalidOperationException($"Instance {key} is operated in progress");

        var tracker = new LaunchTracker(key,
                                        async t => await LaunchInternalAsync((LaunchTracker)t, options),
                                        TrackerOnCompleted);
        _trackers.Add(key, tracker);
        InstanceLaunching?.Invoke(this, tracker);
        tracker.Start();
        return tracker;
    }

    private async Task LaunchInternalAsync(LaunchTracker tracker, LaunchOptions options)
    {
        logger.LogInformation("Begin launch {}", tracker.Key);

        var profile = profileManager.GetImmutable(tracker.Key);

        var artifactPath = PathDef.Default.FileOfLockData(tracker.Key);
        var found = File.Exists(artifactPath);
        if (found)
        {
            var artifact =
                JsonSerializer.Deserialize<DataLock>(await File.ReadAllTextAsync(artifactPath, tracker.Token),
                                                     JsonSerializerOptions.Web);

            if (artifact == null || !artifact.Verify(tracker.Key, profile.Setup))
                throw new InvalidOperationException("Artifact is not valid");


            try
            {
                var javaHome = options.JavaHomeLocator(artifact.JavaMajorVersion);
                var workingDir = PathDef.Default.DirectoryOfBuild(tracker.Key);
                var libraryDir = PathDef.Default.CacheLibraryDirectory;
                var assetDir = PathDef.Default.CacheAssetDirectory;
                var nativeDir = PathDef.Default.DirectoryOfNatives(tracker.Key);
                var igniter = artifact.MakeIgniter();
                igniter
                   .AddGameArgument("--width")
                   .AddGameArgument("${resolution_width}")
                   .AddGameArgument("--height")
                   .AddGameArgument("${resolution_height}");
                igniter
                   .SetJavaHome(javaHome)
                   .SetWorkingDirectory(workingDir)
                   .SetAssetRootDirectory(assetDir)
                   .SetNativesRootDirectory(nativeDir)
                   .SetLibraryRootDirectory(libraryDir)
                   .SetClassPathSeparator(';')
                   .SetLauncherName("Polymerium")
                   .SetLauncherVersion(Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Eternal")
                   .SetOsName(PlatformHelper.GetOsName())
                   .SetOsArch(PlatformHelper.GetOsArch())
                   .SetOsVersion(PlatformHelper.GetOsVersion())
                    // .SetUserUuid(account.Uuid)
                    // .SetUserType(account.UserType)
                    // .SetUserName(account.Username)
                    // .SetUserAccessToken(account.AccessToken)
                   .SetUserName("Steve")
                   .SetUserType("legacy")
                   .SetUserUuid("00000000-0000-0000-0000-000000000000")
                   .SetUserAccessToken("rand(32)")
                   .SetVersionName(profile.Setup.Version)
                   .SetWindowSize(options.WindowSize)
                   .SetMaxMemory(options.MaxMemory)
                   .SetReleaseType("Polymerium");
                foreach (var additional in options.AdditionalArguments.Split(' '))
                    igniter.AddJvmArgument(additional);

                igniter.IsDebug = options.Mode == LaunchMode.FireAndForget;
                var process = igniter.Build();
                await File.WriteAllLinesAsync(Path.Combine(PathDef.Default.DirectoryOfBuild(tracker.Key),
                                                           "trident.launch.dump.txt"),
                                              process.StartInfo.ArgumentList);
                if (options.Mode == LaunchMode.Managed)
                {
                    var launcher = new LaunchEngine(process);
                    await foreach (var scrap in launcher.WithCancellation(tracker.Token).ConfigureAwait(false))
                        tracker.ScrapBuffer.AddLast(scrap);

                    if (tracker.Token.IsCancellationRequested)
                    {
                        if (!tracker.IsDetaching)
                            process.Kill();
                    }
                    else
                    {
                        await process.WaitForExitAsync(tracker.Token);
                    }

                    if (process.ExitCode != 0)
                        throw new Exception($"The process has exited with non-zero code {process.ExitCode}");
                }
                else
                {
                    process.Start();
                }

                // profile.Records.Timeline.Add(new Profile.RecordData.TimelinePoint(true,
                //                                  profile.Reference,
                //                                  Profile.RecordData.TimelinePoint.TimelimeAction.Play,
                //                                  beginTime,
                //                                  DateTimeOffset.Now));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Launch failed due to exception: {ex}", e.Message);
                // profile.Records.Timeline.Add(new Profile.RecordData.TimelinePoint(false,
                //                                  profile.Reference,
                //                                  Profile.RecordData.TimelinePoint.TimelimeAction.Play,
                //                                  beginTime,
                //                                  DateTimeOffset.Now));
                throw;
            }
        }
        else
        {
            throw new ArtifactUnavailableException(tracker.Key, artifactPath, found);
        }
    }

    #endregion

    #region Install

    public InstallTracker Install(string key, string label, string? ns, string pid, string? vid)
    {
        // 只有在线安装会有 Tracker，离线导入因为不需要等待，全在前端进行

        var reserved = profileManager.RequestKey(key);
        var tracker = new InstallTracker(reserved.Key,
                                         async t => await InstallInternalAsync((InstallTracker)t,
                                                                               reserved,
                                                                               label,
                                                                               ns,
                                                                               pid,
                                                                               vid),
                                         TrackerOnCompleted);
        _trackers.Add(reserved.Key, tracker);
        InstanceInstalling?.Invoke(this, tracker);
        tracker.Start();
        return tracker;
    }

    private async Task InstallInternalAsync(
        InstallTracker tracker,
        ReservedKey key,
        string label,
        string? ns,
        string pid,
        string? vid)
    {
        logger.LogInformation("Begin install package {} as {}", PackageHelper.ToPurl(label, ns, pid, vid), key.Key);
        var package = await repositories.ResolveAsync(label,
                                                      ns,
                                                      pid,
                                                      vid,
                                                      Filter.Empty with { Kind = ResourceKind.Modpack });
        var size = package.Size;
        logger.LogDebug("Downloading package file {} sized {} bytes", package.Download.AbsoluteUri, size);
        var client = clientFactory.CreateClient();

        var memory = await DownloadFileAsync(package.Download, size, tracker.ProgressStream, client, tracker.Token);

        logger.LogDebug("Downloaded {} bytes", memory.Length);

        tracker.ProgressStream.OnNext(100d);
        await Task.Delay(TimeSpan.FromSeconds(1));

        tracker.ProgressStream.OnNext(null);
        CompressedProfilePack pack = new(memory) { Reference = package };
        var container = await importers.ImportAsync(pack);

        if (container.IconUrl is not null)
            await ExtractIconFileAsync(key.Key, container, client);


        logger.LogDebug("{} files collected to extract", container.ImportFileNames.Count);

        await importers.ExtractImportFilesAsync(key.Key, container, pack);

        profileManager.Add(key, container.Profile);

        logger.LogInformation("{} added", key.Key);

        client.Dispose();
    }

    #endregion

    #region Update

    public UpdateTracker Update(string key, string label, string? ns, string pid, string vid)
    {
        if (IsInUse(key))
            throw new InvalidOperationException($"Instance {key} is operated in progress");

        var tracker = new UpdateTracker(key,
                                        async t =>
                                            await UpdateInternalAsync((UpdateTracker)t, key, label, ns, pid, vid),
                                        TrackerOnCompleted);
        _trackers.Add(key, tracker);
        InstanceUpdating?.Invoke(this, tracker);
        tracker.Start();
        return tracker;
    }

    private async Task UpdateInternalAsync(
        UpdateTracker tracker,
        string key,
        string label,
        string? ns,
        string pid,
        string vid)
    {
        logger.LogInformation("Begin update {} from package {}", key, PackageHelper.ToPurl(label, ns, pid, vid));
        var package = await repositories.ResolveAsync(label,
                                                      ns,
                                                      pid,
                                                      vid,
                                                      Filter.Empty with { Kind = ResourceKind.Modpack });
        var size = package.Size;
        logger.LogDebug("Downloading package file {} sized {} bytes", package.Download.AbsoluteUri, size);
        var client = clientFactory.CreateClient();

        var memory = await DownloadFileAsync(package.Download, size, tracker.ProgressStream, client, tracker.Token);

        logger.LogDebug("Downloaded {} bytes", memory.Length);

        tracker.ProgressStream.OnNext(100d);
        await Task.Delay(TimeSpan.FromSeconds(1));

        tracker.ProgressStream.OnNext(null);
        CompressedProfilePack pack = new(memory) { Reference = package };
        var container = await importers.ImportAsync(pack);

        if (container.IconUrl is not null)
            await ExtractIconFileAsync(key, container, client);

        logger.LogDebug("{} files collected to extract", container.ImportFileNames.Count);

        var importDir = PathDef.Default.DirectoryOfImport(key);
        // TODO: rename import to import.bak rather than delete and restore if failed
        if (Directory.Exists(importDir))
            Directory.Delete(importDir, true);

        await importers.ExtractImportFilesAsync(key, container, pack);

        profileManager.Update(key,
                              container.Profile.Setup.Source,
                              container.Profile.Name,
                              container.Profile.Setup.Version,
                              container.Profile.Setup.Loader,
                              container.Profile.Setup.Stage.AsReadOnly(),
                              container.Profile.Overrides);

        logger.LogInformation("{} updated", key);

        client.Dispose();
    }

    #endregion
}