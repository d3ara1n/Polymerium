namespace Polymerium.Trident.Services.Instances;

public class DeployOptions(bool? fastMode, bool? resolveDependency)
{
    public bool FastMode { get; set; } = fastMode ?? false;
    public bool ResolveDependency { get; set; } = resolveDependency ?? false;
}