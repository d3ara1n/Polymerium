namespace MirrorChyan.Net.Models;

public record VersionModel(
    UpdateKind Kind,
    ChannelKind Channel,
    string Version,
    int VersionNumber,
    string ReleaseNote,
    ArtifactModel Artifact);
