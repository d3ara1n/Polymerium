using Polymerium.Trident.Services.Instances;

namespace Polymerium.App.Messages
{
    public class InstanceDeployingMessage(string key, DeployTracker tracker)
    {
        public string Key => key;
        public DeployTracker Handle => tracker;
    }
}