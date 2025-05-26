using Polymerium.Trident.Services;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Repositories;

public class ModrinthRepository(ModrinthService service) : IRepository
{
    public string Label => ModrinthService.LABEL;

    public async Task<RepositoryStatus> CheckStatusAsync()
    {
        var loaders = await service.GetLoadersAsync().ConfigureAwait(false);
        var supportedLoaders = loaders
                              .Select(x => ModrinthService.MODLOADER_MAPPINGS.GetValueOrDefault(x))
                              .Where(x => x != null)
                              .Select(x => x!)
                              .ToList();
        var versions = await service.GetGameVersionsAsync().ConfigureAwait(false);
        return new RepositoryStatus(supportedLoaders,
                                    versions,
                                    [
                                        ResourceKind.Modpack,
                                        ResourceKind.Mod,
                                        ResourceKind.ResourcePack,
                                        ResourceKind.ShaderPack
                                    ]);
    }

    public async Task<IPaginationHandle<Exhibit>> SearchAsync(string query, Filter filter)
    {
        return new PaginationHandle<Exhibit>([],
                                             50,
                                             0,
                                             (pageIndex, token) => Task.FromResult(Enumerable.Empty<Exhibit>()));
    }

    public Task<Project> QueryAsync(string? ns, string pid) => throw new NotImplementedException();

    public Task<IEnumerable<Project>> QueryBatchAsync(IEnumerable<(string?, string pid)> batch) =>
        throw new NotImplementedException();

    public Task<Package> ResolveAsync(string? ns, string pid, string? vid, Filter filter) =>
        throw new NotImplementedException();

    public Task<string> ReadDescriptionAsync(string? ns, string pid) => throw new NotImplementedException();

    public Task<string> ReadChangelogAsync(string? ns, string pid, string vid) => throw new NotImplementedException();

    public Task<IPaginationHandle<Version>> InspectAsync(string? ns, string pid, Filter filter) =>
        throw new NotImplementedException();
}