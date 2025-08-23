namespace Polymerium.Trident.Services.Instances
{
    public class DeployOptions(bool? fastMode, bool? resolveDependency, bool? fullCheckMode)
    {
        public bool FastMode { get; set; } = fastMode ?? false;
        public bool ResolveDependency { get; set; } = resolveDependency ?? false;
        public bool FullCheckMod { get; set; } = fullCheckMode ?? false;
    }
}
