namespace Polymerium.Trident.Services.Instances;

public class InstanceDeployingEventArgs(string key, DeployTracker tracker) : EventArgs
{
    public string Key => key;
    public DeployTracker Handle => tracker;
}