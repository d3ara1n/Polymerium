using Trident.Abstractions.Repositories.Resources;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Trident.Abstractions.Repositories;

public interface IRepository
{
    string Label { get; }
    Task<RepositoryStatus> CheckStatusAsync();
    Task<IPaginationHandle<Exhibit>> SearchAsync(string query, Filter filter);
    Task<Project> QueryAsync(string? ns, string pid);
    Task<Package> ResolveAsync(string? ns, string pid, string? vid, Filter filter);
    Task<IPaginationHandle<Version>> InspectAsync(string? ns, string pid, Filter filter);
}