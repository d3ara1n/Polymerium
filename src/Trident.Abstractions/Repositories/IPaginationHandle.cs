namespace Trident.Abstractions.Repositories;

public interface IPaginationHandle<T>
{
    Task<IEnumerable<T>> FetchAsync();

    uint PageSize { get; }
    uint PageIndex { get; set; }

    ulong TotalCount { get; }
}