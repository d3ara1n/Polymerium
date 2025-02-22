using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Services.Instances;
using Polymerium.Trident.Services.Profiles;
using Polymerium.Trident.Utilities;
using Trident.Abstractions.Importers;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Tasks;
using Trident.Abstractions.Utilities;

namespace Polymerium.Trident.Services;

public class InstanceManager(ILogger<InstanceManager> logger, ProfileManager profileManager, RepositoryAgent repositories, ImporterAgent importers, IHttpClientFactory clientFactory)
{
    // 主要是在 UI 线程中增删改查，实际不需要保证线程同步
    private readonly Dictionary<string, TrackerBase> _trackers = new();
    private static readonly string[] INVALID_NAMES = ["", ".", ".."];
    public event EventHandler<InstallTracker>? InstanceInstalling;
    public event EventHandler<UpdateTracker>? InstanceUpdating;

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

    private static async Task<MemoryStream> DownloadFileAsync(Uri download, ulong size, IProgress<double?>? reporter, HttpClient client, CancellationToken token)
    {
        var stream = await client.GetStreamAsync(download, token);
        var memory = new MemoryStream();
        var buffer = new byte[8192];
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

    private static async Task ExtractImportFilesAsync(string key, ImportedProfileContainer container, CompressedProfilePack pack)
    {
        var importDir = PathDef.Default.DirectoryOfImport(key);

        foreach (var (source, target) in container.ImportFileNames.Where(x => !x.Item2.EndsWith('/') && !INVALID_NAMES.Contains(x.Item2)))
        {
            var to = Path.Combine(importDir, target);
            var dir = Path.GetDirectoryName(to);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var fromStream = pack.Open(source);
            var file = new FileStream(to, FileMode.Create);
            await fromStream.CopyToAsync(file);
            await file.FlushAsync();
            file.Close();
            fromStream.Close();
        }
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

    #region Install

    public InstallTracker Install(string key, string label, string? ns, string pid, string? vid)
    {
        // 只有在线安装会有 Tracker，离线导入因为不需要等待，全在前端进行

        var reserved = profileManager.RequestKey(key);
        InstallTracker? tracker = new(reserved.Key, async t => await InstallInternalAsync((InstallTracker)t, reserved, label, ns, pid, vid), TrackerOnCompleted);
        _trackers.Add(reserved.Key, tracker);
        InstanceInstalling?.Invoke(this, tracker);
        tracker.Start();
        return tracker;
    }

    private async Task InstallInternalAsync(InstallTracker tracker, ReservedKey key, string label, string? ns, string pid, string? vid)
    {
        logger.LogInformation("Begin install package {} as {}", PackageHelper.ToPurl(label, ns, pid, vid), key.Key);
        var package = await repositories.ResolveAsync(label, ns, pid, vid, Filter.Empty with { Kind = ResourceKind.Modpack });
        var size = package.Size;
        logger.LogDebug("Downloading package file {} sized {} bytes", package.Download.AbsoluteUri, size);
        var client = clientFactory.CreateClient();

        var memory = await DownloadFileAsync(package.Download, size, tracker, client, tracker.Token);

        logger.LogDebug("Downloaded {} bytes", memory.Length);

        ((IProgress<double?>)tracker).Report(null);
        CompressedProfilePack pack = new(memory) { Reference = package };
        var container = await importers.ImportAsync(pack);

        if (container.IconUrl is not null)
            await ExtractIconFileAsync(key.Key, container, client);


        logger.LogDebug("{} files collected to extract", container.ImportFileNames.Count);

        await ExtractImportFilesAsync(key.Key, container, pack);

        profileManager.Add(key, container.Profile);

        logger.LogInformation("{} added", key.Key);

        client.Dispose();
    }

    #endregion

    #region Update

    public void Update(string key, string label, string? ns, string pid, string vid)
    {
        if (IsInUse(key))
            throw new InvalidOperationException($"Instance {key} is operated in progress");

        UpdateTracker tracker = new(key, async t => await UpdateInternalAsync((UpdateTracker)t, key, label, ns, pid, vid), TrackerOnCompleted);
        _trackers.Add(key, tracker);
        InstanceUpdating?.Invoke(this, tracker);
        tracker.Start();
    }

    private async Task UpdateInternalAsync(UpdateTracker tracker, string key, string label, string? ns, string pid, string vid)
    {
        logger.LogInformation("Begin update {} from package {}", key, PackageHelper.ToPurl(label, ns, pid, vid));
        var package = await repositories.ResolveAsync(label, ns, pid, vid, Filter.Empty with { Kind = ResourceKind.Modpack });
        var size = package.Size;
        logger.LogDebug("Downloading package file {} sized {} bytes", package.Download.AbsoluteUri, size);
        var client = clientFactory.CreateClient();

        var memory = await DownloadFileAsync(package.Download, size, tracker, client, tracker.Token);

        logger.LogDebug("Downloaded {} bytes", memory.Length);

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

        await ExtractImportFilesAsync(key, container, pack);

        profileManager.Update(key, container.Profile.Setup.Source, container.Profile.Name, container.Profile.Setup.Version, container.Profile.Setup.Loader, container.Profile.Setup.Stage.AsReadOnly(), container.Profile.Overrides);

        logger.LogInformation("{} updated", key);

        client.Dispose();
    }

    #endregion
}