namespace Polymerium.Trident.Models.ModrinthApi;

public record SearchResponse<T>(IReadOnlyList<T> Hits, uint Offset, uint Limit, uint TotalHits);