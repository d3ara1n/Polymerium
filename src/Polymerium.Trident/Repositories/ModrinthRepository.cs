using Polymerium.Trident.Services;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Repositories;

public class ModrinthRepository(ModrinthService service) : IRepository
{
    private const uint PAGE_SIZE = 20;
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
                         .SearchAsync(query,
                                      ModrinthService.ResourceKindToType(filter.Kind),
                                      filter.Version,
                                      loader,
                                      limit: PAGE_SIZE)
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
                                                                            pageIndex * first.Limit,
                                                                            first.Limit)
                                                               .ConfigureAwait(false);
                                                 var exhibits = rv.Hits.Select(ModrinthService.ToExhibit).ToList();
                                                 return exhibits;
                                             });
    }

    public async Task<Project> QueryAsync(string? ns, string pid)
    {
        var project = await service.GetProjectAsync(pid).ConfigureAwait(false);
        var team = await service.GetTeamMembersAsync(project.TeamId).ConfigureAwait(false);
        return ModrinthService.ToProject(project, team.FirstOrDefault());
    }

    public Task<IEnumerable<Project>> QueryBatchAsync(IEnumerable<(string?, string pid)> batch) =>
        throw new NotImplementedException();

    public Task<Package> ResolveAsync(string? ns, string pid, string? vid, Filter filter) =>
        throw new NotImplementedException();

    public async Task<string> ReadDescriptionAsync(string? ns, string pid)
    {
        var project = await service.GetProjectAsync(pid).ConfigureAwait(false);
        return project.Description;
    }

    public Task<string> ReadChangelogAsync(string? ns, string pid, string vid) => throw new NotImplementedException();

    public async Task<IPaginationHandle<Version>> InspectAsync(string? ns, string pid, Filter filter)
    {
        var project = await service.GetProjectAsync(pid).ConfigureAwait(false);
        var type = project.ProjectTypes.FirstOrDefault();
        var loader = type == ModrinthService.RESOURCENAME_MOD ? ModrinthService.LoaderIdToName(filter.Loader) : null;
        var first = await service
                         .GetProjectVersionsAsync(pid, filter.Version, loader, limit: PAGE_SIZE)
                         .ConfigureAwait(false);
        var initial = first.Select(ModrinthService.ToVersion);
        // Modrinth 没有分页响应，TotalCount 没法获取，好在 PaginationHandle 和 InfiniteCollection 都不依赖这个值
        return new PaginationHandle<Version>(initial,
                                             PAGE_SIZE,
                                             0,
                                             async (pageIndex, _) =>
                                             {
                                                 var rv = await service
                                                               .GetProjectVersionsAsync(pid,
                                                                    filter.Version,
                                                                    loader,
                                                                    pageIndex * PAGE_SIZE,
                                                                    PAGE_SIZE)
                                                               .ConfigureAwait(false);
                                                 return rv.Select(ModrinthService.ToVersion);
                                             });
    }
}