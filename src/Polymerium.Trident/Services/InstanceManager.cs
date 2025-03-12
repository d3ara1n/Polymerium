using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Engines;
using Polymerium.Trident.Engines.Deploying.Stages;
using Polymerium.Trident.Services.Instances;
using Polymerium.Trident.Services.Profiles;
using Polymerium.Trident.Utilities;
using Trident.Abstractions;
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

    private void TrackerOnCompleted(TrackerBase tracker) => _trackers.Remove(tracker.Key);

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

    #region Common

    private static async Task<MemoryStream> DownloadFileAsync(
        Uri download,
        ulong size,
        IProgress<double?>? reporter,
        HttpClient client,
        CancellationToken token)
    {
        var stream = await client.GetStreamAsync(download, token);
        var memory = new MemoryStream();
        var buffer = new byte[8*1024];
        int read;
        var totalRead = 0L;
        do
        {
            read = await stream.ReadAsync(buffer, token);
            await memory.WriteAsync(buffer.AsMemory(0, read), token);
            totalRead += read;
            var progress = (double)(totalRead * 100) / size;
            reporter?.Report(progress);
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

    public DeployTracker Deploy(string key, DeployOptions options)
    {
        if (IsInUse(key))
            throw new InvalidOperationException($"Instance {key} is operated in progress");

        var tracker = new DeployTracker(key,
                                        async t => await DeployInternalAsync((DeployTracker)t, key, options),
                                        TrackerOnCompleted);
        _trackers.Add(key, tracker);
        InstanceDeploying?.Invoke(this, tracker);
        tracker.Start();
        return tracker;
    }

    private async Task DeployInternalAsync(DeployTracker tracker, string key, DeployOptions options)
    {
        logger.LogInformation("Begin deploy {}", key);

        if (!profileManager.TryGetImmutable(key, out var profile))
            throw new KeyNotFoundException($"{key} is not a key to the managed profile");


        var engine = new DeployEngine(key, profile.Setup, provider, options);

        var watch = Stopwatch.StartNew();
        IProgress<DeployTracker.DeployProgress> reporter = tracker;
        foreach (var stage in engine)
        {
            if (tracker.Token.IsCancellationRequested)
                break;
            switch (stage)
            {
                case CheckArtifactStage:
                    reporter.Report(new DeployTracker.DeployProgress("Checking artifacts..."));
                    break;
                case ProcessLoaderStage:
                    reporter.Report(new DeployTracker.DeployProgress("Installing loader..."));
                    break;
                case ResolvePackageStage resolvePackageStage:
                    reporter.Report(new DeployTracker.DeployProgress("Resolving packages..."));
                    resolvePackageStage.ProgressReporter =
                        new Progress<(int, int)>(x =>
                                                     reporter
                                                        .Report(new
                                                                    DeployTracker.
                                                                    DeployProgress($"Resolving packages...({x.Item1}/{x.Item2})",
                                                                        (double)x.Item1 * 100 / x.Item2)));
                    break;
                case BuildArtifactStage:
                    reporter.Report(new DeployTracker.DeployProgress("Building artifacts..."));
                    break;
                case GenerateManifestStage:
                    reporter.Report(new DeployTracker.DeployProgress("Generating manifest..."));
                    break;
                case SolidifyManifestStage solidifyManifestStage:
                    reporter.Report(new DeployTracker.DeployProgress("Solidifying files..."));
                    solidifyManifestStage.ProgressReporter =
                        new Progress<(int, int)>(x =>
                                                     reporter
                                                        .Report(new
                                                                    DeployTracker.
                                                                    DeployProgress($"Solidifying files...({x.Item1}/{x.Item2})",
                                                                        (double)x.Item1 * 100 / x.Item2)));
                    break;
            }

            logger.LogInformation("Enter stage {}", stage.GetType().Name);
            await stage.ProcessAsync(tracker.Token);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        watch.Stop();
        logger.LogInformation("{} deployed in {}ms", key, watch.ElapsedMilliseconds);
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

        var memory = await DownloadFileAsync(package.Download, size, tracker, client, tracker.Token);

        logger.LogDebug("Downloaded {} bytes", memory.Length);

        ((IProgress<double?>)tracker).Report(100d);
        await Task.Delay(TimeSpan.FromSeconds(1));

        ((IProgress<double?>)tracker).Report(null);
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

        var memory = await DownloadFileAsync(package.Download, size, tracker, client, tracker.Token);

        logger.LogDebug("Downloaded {} bytes", memory.Length);

        ((IProgress<double?>)tracker).Report(100d);
        await Task.Delay(TimeSpan.FromSeconds(1));

        ((IProgress<double?>)tracker).Report(null);
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