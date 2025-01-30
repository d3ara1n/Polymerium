using System.Diagnostics.CodeAnalysis;
using Polymerium.Trident.Services.Instances;
using Polymerium.Trident.Services.Profiles;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services;

public class InstanceManager(ProfileManager profileManager, RepositoryAgent agent, IHttpClientFactory clientFactory)
{
    // 主要是在 UI 线程中增删改查，实际不需要保证线程同步
    private readonly Dictionary<string, TrackerBase> _trackers = new();

    public event EventHandler<InstallTracker>? InstanceInstalling;

    public InstallTracker Install(string key, string label, string? ns, string pid, string? vid)
    {
        // 只有在线安装会有 Tracker，离线导入因为不需要等待，全在前端进行

        var reserved = profileManager.RequestKey(key);
        var tracker = new InstallTracker(reserved.Key,
            async t => await InstallInternalAsync(t, reserved, label, ns, pid, vid));
        _trackers.Add(key, tracker);
        InstanceInstalling?.Invoke(this, tracker);
        return tracker;
    }

    private async Task InstallInternalAsync(TrackerBase tracker, ReservedKey key, string label, string? ns,
        string pid, string? vid)
    {
        if (tracker is not InstallTracker install) throw new InvalidOperationException();
        var package = await agent.ResolveAsync(label, ns, pid, vid, Filter.Empty with
        {
            Kind = ResourceKind.Modpack
        });
        var client = clientFactory.CreateClient();
        await using var stream = await client.GetStreamAsync(package.Download);
        var size = stream.Length != 0 ? stream.Length : (long)package.Size;
        using var writer = new MemoryStream();
        var buffer = new byte[8192];
        var read = 0;
        var totalRead = 0;
        while (!install.Token.IsCancellationRequested && (read = await stream.ReadAsync(buffer)) > 0)
        {
            await writer.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;
            install.Report((double)(totalRead * 100) / size);
        }
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

    public bool IsInUse(string key)
    {
        return _trackers.ContainsKey(key);
    }
}