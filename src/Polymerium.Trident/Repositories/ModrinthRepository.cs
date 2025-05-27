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
        var loader = filter.Kind is ResourceKind.Mod ? ModrinthService.LoaderIdToName(filter.Loader) : null;
        var first = await service
                         .SearchAsync(query, ModrinthService.ResourceKindToType(filter.Kind), filter.Version, loader)
                         .ConfigureAwait(false);
        var initial = first.Hits.Select(ModrinthService.ToExhibit);
        return new PaginationHandle<Exhibit>(initial,
                                             first.Limit,
                                             first.TotalHits,
                                             async (pageIndex, _) =>
                                             {
                                                 var rv = await service
                                                               .SearchAsync(query,
                                                                            ModrinthService
                                                                               .ResourceKindToType(filter.Kind),
                                                                            filter.Version,
                                                                            loader,
                                                                            pageIndex * first.Limit)
                                                               .ConfigureAwait(false);
                                                 var exhibits = rv.Hits.Select(ModrinthService.ToExhibit).ToList();
                                                 return exhibits;
                                             });
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