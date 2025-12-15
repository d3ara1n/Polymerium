namespace MirrorChyan.Net.Models;

public record ArtifactModel(UpdateKind Kind, Uri Url, string Sha256, int FileSize, string Os, string Arch);
