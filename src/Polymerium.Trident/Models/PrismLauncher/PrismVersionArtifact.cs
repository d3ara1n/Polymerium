namespace Polymerium.Trident.Models.PrismLauncher;

public struct PrismVersionArtifact
{
    public string Sha1 { get; init; }
    public uint Size { get; init; }
    public Uri Url { get; init; }
}