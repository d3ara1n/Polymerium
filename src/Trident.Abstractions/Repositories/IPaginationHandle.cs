namespace Trident.Abstractions.Repositories;

public interface IPaginationHandle<T>
{
    uint PageSize { get; }
    uint PageIndex { get; set; }

    ulong TotalCount { get; }
    Task<IEnumerable<T>> FetchAsync(CancellationToken token);
}