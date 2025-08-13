namespace Polymerium.Trident.Models.ModrinthApi
{
    public readonly record struct SearchResponse<T>(IReadOnlyList<T> Hits, uint Offset, uint Limit, uint TotalHits);
}
