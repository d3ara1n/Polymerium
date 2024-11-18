using Trident.Abstractions.Repositories.Resources;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Trident.Abstractions.Repositories;

public interface IRepository
{
    Task<IPaginationHandle<Exhibit>> SearchAsync(string query, Filter filter);
    Task<Project> QueryAsync(string? ns, string name);
    Task<Package> ResolveAsync(string? ns, string name, string? version, Filter filter);
    Task<IPaginationHandle<Version>> InspectAsync(string? ns, string name, Filter filter);
}