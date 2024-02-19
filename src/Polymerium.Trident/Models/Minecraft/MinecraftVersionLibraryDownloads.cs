namespace Polymerium.Trident.Models.Minecraft
{
    public struct MinecraftVersionLibraryDownloads
    {
        public MinecraftVersionArtifact? Artifact { get; init; }
        public IDictionary<string, MinecraftVersionArtifact> Classifiers { get; init; }
    }
}