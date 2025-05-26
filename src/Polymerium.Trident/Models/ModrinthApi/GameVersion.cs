namespace Polymerium.Trident.Models.ModrinthApi;

public record GameVersion(string Version, string VersionType, DateTimeOffset Date, bool Major);