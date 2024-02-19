using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Repositories
{
    public class ModrinthRepository : IRepository
    {
        public string Label => RepositoryLabels.MODRINTH;

        public Task<Project> QueryAsync(string projectId, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<Package> ResolveAsync(string projectId, string? versionId, Filter filter,
            CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Exhibit>> SearchAsync(string keyword, uint page, uint limit, Filter filter,
            CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}