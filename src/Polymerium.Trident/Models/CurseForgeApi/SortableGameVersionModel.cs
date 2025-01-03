namespace Polymerium.Trident.Models.CurseForgeApi;

public record SortableGameVersionModel(
    string GameVersionName,
    string GameVersionPadded,
    string GameVersion,
    DateTimeOffset GameVersionReleaseDate,
    uint GameVersionTypeId);