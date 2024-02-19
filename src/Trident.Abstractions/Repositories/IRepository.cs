using Trident.Abstractions.Resources;

namespace Trident.Abstractions.Repositories
{
    public interface IRepository
    {
        public string Label { get; }
        public Task<Project> QueryAsync(string projectId, CancellationToken token);

        public Task<Package> ResolveAsync(string projectId, string? versionId, Filter filter,
            CancellationToken token);

        public Task<IEnumerable<Exhibit>> SearchAsync(string keyword, uint page, uint limit, Filter filter,
            CancellationToken token);
    }
}