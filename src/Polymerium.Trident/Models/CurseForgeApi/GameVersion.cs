namespace Polymerium.Trident.Models.CurseForgeApi;

public record GameVersion(
    uint Id,
    uint GameId,
    string VersionString,
    Uri JarDownloadUrl,
    Uri JsonDownloadUrl,
    bool Approved,
    DateTimeOffset DateModified,
    uint GameVersionTypeId,
    uint GameVersionStatus,
    uint GameVersionTypeStatus);