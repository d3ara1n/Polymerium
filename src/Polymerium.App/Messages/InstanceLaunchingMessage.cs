using Polymerium.Trident.Launching;
using Polymerium.Trident.Services.Instances;

namespace Polymerium.App.Messages
{
    public class InstanceLaunchingMessage(string key, LaunchTracker tracker, LaunchMode mode)
    {
        public string Key => key;
        public LaunchTracker Handle => tracker;
        public LaunchMode Mode => mode;
    }
}