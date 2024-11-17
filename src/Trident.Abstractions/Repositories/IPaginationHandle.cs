namespace Trident.Abstractions.Repositories;

public interface IPaginationHandle<T>
{
    Task<IEnumerable<T>> FetchAsync();
}