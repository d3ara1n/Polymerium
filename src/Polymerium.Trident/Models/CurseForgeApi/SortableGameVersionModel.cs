namespace Polymerium.Trident.Models.CurseForgeApi
{
    public readonly record struct SortableGameVersionModel(
        string GameVersionName,
        string GameVersionPadded,
        string GameVersion,
        DateTimeOffset GameVersionReleaseDate,
        uint? GameVersionTypeId);
}
