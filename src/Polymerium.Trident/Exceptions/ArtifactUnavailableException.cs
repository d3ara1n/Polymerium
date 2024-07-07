namespace Polymerium.Trident.Exceptions;

public class ArtifactUnavailableException : Exception
{
    public ArtifactUnavailableException(string key, string artifactPath, bool exist) : base(
        $"Artifact of {key} is not available, maybe not built or outdated")
    {
        Key = key;
        ArtifactPath = artifactPath;
        Exist = exist;
    }

    public string Key { get; }
    public string ArtifactPath { get; }
    public bool Exist { get; }
}