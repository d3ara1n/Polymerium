using Trident.Abstractions.Resources;

namespace Trident.Abstractions.Repositories;

public struct SearchResultSet
{
    public IEnumerable<Exhibit> Results { get; init; }
    public int Page { get; init; }
    public int Limit { get; init; }
    public int Total { get; init; }
}