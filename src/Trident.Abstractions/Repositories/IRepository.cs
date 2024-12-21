using Trident.Abstractions.Repositories.Resources;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Trident.Abstractions.Repositories;

public interface IRepository
{
    Task<IPaginationHandle<Exhibit>> SearchAsync(string query, Filter filter);
    Task<Project> QueryAsync(string? @namespace, string name);
    Task<Package> ResolveAsync(string? @namespace, string name, string? version, Filter filter);
    Task<IPaginationHandle<Version>> InspectAsync(string? @namespace, string name, Filter filter);
}