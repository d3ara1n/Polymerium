using Polymerium.Trident.Services;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Repositories;

public class CurseForgeRepository(CurseForgeService service) : IRepository
{
    public string Label { get; } = CurseForgeService.LABEL;

    public async Task<RepositoryStatus> CheckStatusAsync()
    {
        var versions = await service.GetGameVersionsAsync();
        return new RepositoryStatus([
            LoaderHelper.LOADERID_NEOFORGE, LoaderHelper.LOADERID_FORGE, LoaderHelper.LOADERID_FABRIC,
            LoaderHelper.LOADERID_QUILT
        ], versions);
    }

    public async Task<IPaginationHandle<Exhibit>> SearchAsync(string query, Filter filter)
    {
        var first = await service.SearchAsync(query, service.ResourceKindToClassId(filter.Kind), filter.Version,
            service.LoaderIdToType(filter.Loader));
        var initial = first.Data.Select(service.ModModelToExhibit);
        return new PaginationHandle<Exhibit>(initial, 50, first.Pagination.TotalCount, async index =>
        {
            var rv = await service.SearchAsync(query, service.ResourceKindToClassId(filter.Kind), filter.Version,
                service.LoaderIdToType(filter.Loader), index);
            var exhibits = rv.Data.Select(service.ModModelToExhibit).ToList();
            return exhibits;
        });
    }

    public Task<Project> QueryAsync(string? @namespace, string name)
    {
        throw new NotImplementedException();
    }

    public Task<Package> ResolveAsync(string? @namespace, string name, string? version, Filter filter)
    {
        throw new NotImplementedException();
    }

    public Task<IPaginationHandle<Version>> InspectAsync(string? @namespace, string name, Filter filter)
    {
        throw new NotImplementedException();
    }
}