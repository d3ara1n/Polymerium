namespace Trident.Abstractions.Repositories;

public interface IRepository
{
    Task<IPaginationHandle<object>> SearchAsync(string query, Filter filter);
    Task<object> ResolveAsync(string? ns, string name, string? version, Filter filter);
    Task<IPaginationHandle<Version>> QueryAsync(string? ns, string name, Filter filter);
    Task<FileInfo> FetchAsync(string? ns, string name, string version);
}