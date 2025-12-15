namespace MirrorChyan.Net.Models;

public record ArtifactModel(Uri Url, string Sha256, int FileSize, string Os, string Arch);
