namespace Polymerium.Trident.Models.PrismLauncher
{
    public struct PrismIndexVersion
    {
        public bool Recommended { get; init; }
        public DateTimeOffset ReleaseTime { get; init; }
        public PrismRequirement[] Requires { get; init; }
        public string Sha256 { get; init; }
        public PrismReleaseType Type { get; init; }
        public string Version { get; init; }
    }
}