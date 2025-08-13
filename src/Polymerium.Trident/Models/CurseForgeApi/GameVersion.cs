namespace Polymerium.Trident.Models.CurseForgeApi;

public readonly record struct GameVersion(
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