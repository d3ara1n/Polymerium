namespace MirrorChyan.Net.Models;

public record VersionModel(
    ChannelKind Channel,
    string Version,
    int VersionNumber,
    string ReleaseNote,
    ArtifactModel? Artifact);
