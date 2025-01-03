using Trident.Abstractions.Repositories;

namespace Polymerium.Trident.Repositories;

public class PaginationHandle<T>(
    IEnumerable<T> initial,
    uint pageSize,
    uint totalCount,
    Func<uint, Task<IEnumerable<T>>> next) : IPaginationHandle<T>
{
    private IEnumerable<T> _currentItems = initial;
    private readonly uint _currentPage = 0;

    public async Task<IEnumerable<T>> FetchAsync()
    {
        if (_currentPage == PageIndex && _currentItems.Any()) return _currentItems;

        var rv = await next(PageIndex);
        var currentItems = rv as T[] ?? rv.ToArray();
        _currentItems = currentItems;
        return currentItems;
    }

    public uint PageSize => pageSize;
    public uint PageIndex { get; set; } = 0;
    public ulong TotalCount => totalCount;
}