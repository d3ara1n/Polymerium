namespace Polymerium.Trident.Models.ModrinthPack;

public record Index(
    int FormatVersion,
    string Game,
    string VersionId,
    string Name,
    string? Summary,
    IReadOnlyList<Index.IndexFile> Files,
    IDictionary<string, string> Dependencies)
{
    public record IndexFile(
        string Path,
        IndexFile.FileHashes Hashes,
        IndexFile.FileEnv? Env,
        IReadOnlyList<Uri> Downloads,
        ulong FileSize)
    {
        public record FileHashes(string Sha1, string Sha512);

        public record FileEnv(string Client, string Server);
    }
}