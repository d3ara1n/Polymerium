namespace Polymerium.Trident.Models.PrismLauncher
{
    public struct PrismIndex
    {
        public int FormatVersion { get; init; }
        public string Name { get; init; }
        public string Uid { get; init; }
        public PrismIndexVersion[] Versions { get; init; }
    }
}