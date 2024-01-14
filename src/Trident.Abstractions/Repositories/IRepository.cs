using DotNext;
using Trident.Abstractions.Errors;
using Trident.Abstractions.Resources;

namespace Trident.Abstractions.Repositories;

public interface IRepository
{
    public string Label { get; }
    public Task<Result<Project, ResourceError>> QueryAsync(string projectId, CancellationToken token);

    public Task<Result<Package, ResourceError>> ResolveAsync(string projectId, string? versionId, Filter filter,
        CancellationToken token);

    public Task<IEnumerable<Exhibit>> SearchAsync(string keyword, uint page, uint limit, Filter filter,
        CancellationToken token);
}