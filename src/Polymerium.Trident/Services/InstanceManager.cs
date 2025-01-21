using System.Diagnostics.CodeAnalysis;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services;

public class InstanceManager
{
    // 主要是在 UI 线程中增删改查，实际不需要保证线程同步
    private readonly Dictionary<string, TrackerBase> _trackers = new();

    public EventHandler<InstanceInstallEventArgs>? InstanceInstalling;

    public InstallTracker Install(string key)
    {
        throw new NotImplementedException();
    }

    public bool IsTracking<T>(string key, [MaybeNullWhen(false)] out T tracker) where T : TrackerBase
    {
        if (_trackers.TryGetValue(key, out var value) && value is T result)
        {
            tracker = result;
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