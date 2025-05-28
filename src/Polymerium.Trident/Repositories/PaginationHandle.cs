using Trident.Abstractions.Repositories;

namespace Polymerium.Trident.Repositories;

public class PaginationHandle<T>(
    IEnumerable<T> initial,
    uint pageSize,
    uint totalCount,
    Func<uint, CancellationToken, Task<IEnumerable<T>>> next) : IPaginationHandle<T>
{
    private IEnumerable<T> _currentItems = initial;
    private uint _currentPage;

    #region IPaginationHandle<T> Members

    public async Task<IEnumerable<T>> FetchAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return [];
        if (_currentPage == PageIndex && _currentItems.Any())
            return _currentItems;

        var rv = await next(PageIndex, token).ConfigureAwait(false);
        var currentItems = rv as IReadOnlyList<T> ?? rv.ToList();
        _currentItems = currentItems;
        _currentPage = PageIndex;
        return currentItems;
    }

    public uint PageSize => pageSize;
    public uint PageIndex { get; set; } = 0;
    public ulong TotalCount => totalCount;

    #endregion
}