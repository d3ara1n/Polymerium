namespace Polymerium.Trident.Models.CurseForgeApi;

public record GetVersionsResponse(uint Type, IReadOnlyList<string> Versions);