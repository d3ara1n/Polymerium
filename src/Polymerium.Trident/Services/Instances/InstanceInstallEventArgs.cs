namespace Polymerium.Trident.Services.Instances;

public class InstanceInstallEventArgs(string key) : EventArgs
{
    public string Key => key;
}