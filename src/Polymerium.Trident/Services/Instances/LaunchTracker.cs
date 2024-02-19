using System.Diagnostics;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services.Instances
{
    public class LaunchTracker(
        string key,
        TrackerHandler handler,
        Action<TrackerBase> onCompleted,
        CancellationToken token = default) : TrackerBase(key, handler, onCompleted, token)
    {
        public event LaunchFiredHandler? Fired;

        internal void OnLaunched(Process process)
        {
            // start track
            Fired?.Invoke(this);
        }
    }
}