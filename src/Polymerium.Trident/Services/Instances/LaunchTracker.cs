using Polymerium.Trident.Engines.Launching;
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
        public event LaunchOutputHandler? Output;
        public event LaunchExitedHandler? Exited;

        internal void OnFired()
        {
            Fired?.Invoke(this);
        }


        internal void OnOutput(Scrap scrap)
        {
            Output?.Invoke(this, scrap);
        }

        internal void OnExited(int code)
        {
            Exited?.Invoke(this, code);
        }
    }
}