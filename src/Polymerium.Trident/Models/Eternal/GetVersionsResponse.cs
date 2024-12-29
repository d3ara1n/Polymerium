namespace Polymerium.Trident.Models.Eternal;

public record GetVersionsResponse(uint Type, IReadOnlyList<string> Versions);