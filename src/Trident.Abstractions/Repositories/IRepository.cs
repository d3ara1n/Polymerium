namespace Trident.Abstractions.Repositories;

public interface IRepository
{
    Task<IAsyncEnumerable<string>> SearchAsync(string query, Filter filter);
}