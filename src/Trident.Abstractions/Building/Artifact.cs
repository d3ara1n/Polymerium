namespace Trident.Abstractions.Building;

public record Artifact(ArtifactViability Viability, string MainClass)
{
    public static ArtifactBuilder Builder()
    {
        return new ArtifactBuilder();
    }

    public bool Verify(string key, string watermark, string home)
    {
        return Viability.Key == key && Viability.Watermark == watermark && Viability.Home == home;
    }
}