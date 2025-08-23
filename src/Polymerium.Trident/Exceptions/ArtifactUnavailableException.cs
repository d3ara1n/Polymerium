namespace Polymerium.Trident.Exceptions
{
    public class ArtifactUnavailableException(string key, string artifactPath, bool exist)
        : Exception($"Artifact of {key} is not available, maybe not built or outdated")
    {
        public string Key { get; } = key;
        public string ArtifactPath { get; } = artifactPath;
        public bool Exist { get; } = exist;
    }
}
