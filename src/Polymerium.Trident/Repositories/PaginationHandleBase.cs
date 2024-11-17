using Trident.Abstractions.Repositories;

namespace Polymerium.Trident.Repositories;

public abstract class PaginationHandleBase<T> : IPaginationHandle<T>
{
    public abstract Task<IEnumerable<T>> FetchAsync();

    protected IEnumerable<T> Finish() => [];
}