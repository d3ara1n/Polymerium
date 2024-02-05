namespace Trident.Abstractions.Building;

public record Artifact(string MainClassPath)
{
    public static ArtifactBuilder Builder()
    {
        return new ArtifactBuilder();
    }
}