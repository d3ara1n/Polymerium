namespace MirrorChyan.Net.Models;

public record GetLatestVersionResponseBody(
    string VersionName,
    int VersionNumber,
    Uri? Url,
    int? FileSize,
    string? Sha256,
    UpdateKind? UpdateType,
    string Os,
    string Arch,
    string Channel,
    string ReleaseNote,
    int? CdkExpiredTime);
