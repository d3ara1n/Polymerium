using Polymerium.Trident.Launching;

namespace Polymerium.Trident.Services.Instances
{
    public class InstanceLaunchingEventArgs(string key, LaunchTracker tracker, LaunchMode mode) : EventArgs
    {
        public string Key => key;
        public LaunchTracker Handle => tracker;
        public LaunchMode Mode => mode;
    }
}