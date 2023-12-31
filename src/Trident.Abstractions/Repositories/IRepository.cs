using Trident.Abstractions.Resources;

namespace Trident.Abstractions.Repositories
{
    public interface IRepository
    {
        public string Label { get; }
        public Project Query(string projectId);
        public Package Resolve(string projectId, string? versionId, Filter filter);
        public Task<IEnumerable<Exhibit>> SearchAsync(string keyword, uint page, uint limit, Filter filter, CancellationToken token);
    }
}
