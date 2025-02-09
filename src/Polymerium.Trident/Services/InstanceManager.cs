using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Services.Instances;
using Polymerium.Trident.Services.Profiles;
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
    IHttpClientFactory clientFactory)
{
    // 主要是在 UI 线程中增删改查，实际不需要保证线程同步
    private readonly Dictionary<string, TrackerBase> _trackers = new();

    public event EventHandler<InstallTracker>? InstanceInstalling;

    #region Install

    public InstallTracker Install(string key, string label, string? ns, string pid, string? vid)
    {
        // 只有在线安装会有 Tracker，离线导入因为不需要等待，全在前端进行

        var reserved = profileManager.RequestKey(key);
        var tracker = new InstallTracker(reserved.Key,
            async t => await InstallInternalAsync((InstallTracker)t, reserved, label, ns, pid, vid),
            t => _trackers.Remove(t.Key));
        _trackers.Add(reserved.Key, tracker);
        InstanceInstalling?.Invoke(this, tracker);
        tracker.Start();
        return tracker;
    }

    private async Task InstallInternalAsync(InstallTracker tracker, ReservedKey key, string label, string? ns,
        string pid, string? vid)
    {
        logger.LogInformation("Begin install package {} as {}", PackageHelper.ToPurl(label, ns, pid, vid), key.Key);
        var package = await repositories.ResolveAsync(label, ns, pid, vid, Filter.Empty with
        {
            Kind = ResourceKind.Modpack
        });
        var size = (long)package.Size;
        logger.LogDebug("Downloading package file {} sized {} bytes", package.Download.AbsoluteUri, size);
        var client = clientFactory.CreateClient();
        var stream = await client.GetStreamAsync(package.Download);
        var memory = new MemoryStream();
        var buffer = new byte[8192];
        var read = 0;
        var totalRead = 0L;
        do
        {
            read = await stream.ReadAsync(buffer, tracker.Token);
            await memory.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;
            var progress = (double)(totalRead * 100) / size;
            ((IProgress<double?>)tracker).Report(progress);
        } while (!tracker.Token.IsCancellationRequested && read > 0);

        await stream.DisposeAsync();
        memory.Position = 0;

        logger.LogDebug("Downloaded {} bytes", memory.Length);

        ((IProgress<double?>)tracker).Report(null);
        var pack = new CompressedProfilePack(memory)
        {
            Reference = package
        };
        var container = await importers.ImportAsync(pack);
        if (container.IconUrl is not null)
        {
            var iconPath = PathDef.Default.FileOfIcon(key.Key);
            var dir = Path.GetDirectoryName(iconPath);
            if (dir is not null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var iconReader = await client.GetStreamAsync(container.IconUrl);
            var iconWriter = new FileStream(iconPath, FileMode.Create);
            await iconReader.CopyToAsync(iconWriter);
            await iconReader.DisposeAsync();
            await iconWriter.FlushAsync();
            iconWriter.Close();
        }

        logger.LogDebug("{} files collected to extract", container.ImportFileNames.Count);

        foreach (var (source, target) in container.ImportFileNames)
        {
            var to = Path.Combine(PathDef.Default.DirectoryOfImport(key.Key), target);
            var dir = Path.GetDirectoryName(to);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var fromStream = pack.Open(source);
            var file = new FileStream(to, FileMode.Create);
            await fromStream.CopyToAsync(file);
            await fromStream.DisposeAsync();
            await file.FlushAsync();
            await file.DisposeAsync();
            file.Close();
        }

        profileManager.Add(key, container.Profile);

        logger.LogInformation("{} added", key.Key);

        client.Dispose();
        await Task.Delay(2000);
    }

    #endregion

    #region Update

    public void Update(string key)
    {
        if (IsInUse(key)) throw new InvalidOperationException($"Instance {key} is operated in progress");
    }

    private void UpdateInternalAsync(UpdateTracker tracker, string key)
    {
    }

    #endregion

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

    public bool IsInUse(string key)
    {
        return _trackers.ContainsKey(key);
    }
}