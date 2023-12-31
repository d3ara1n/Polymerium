using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Repositories
{
    public class ModrinthRepository : IRepository
    {
        public string Label => RepositoryLabels.MODRINTH;

        public Project Query(string projectId)
        {
            throw new NotImplementedException();
        }

        public Package Resolve(string projectId, string? versionId, Filter filter)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Exhibit>> SearchAsync(string keyworad, uint page, uint limit, Filter filter, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
