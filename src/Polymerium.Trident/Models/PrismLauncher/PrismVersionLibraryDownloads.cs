namespace Polymerium.Trident.Models.PrismLauncher;

public struct PrismVersionLibraryDownloads
{
    public PrismVersionArtifact? Artifact { get; init; }
    public IDictionary<string, PrismVersionArtifact> Classifiers { get; init; }
}