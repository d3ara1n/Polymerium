using Polymerium.Trident.Services;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Repositories;

public class CurseForgeRepository(CurseForgeService service) : IRepository
{
    public string Label { get; } = CurseForgeService.NAME.ToLower();

    public async Task<RepositoryStatus> CheckStatusAsync()
    {
        var versions = await service.GetGameVersionsAsync();
        return new RepositoryStatus([
            LoaderHelper.LOADERID_NEOFORGE, LoaderHelper.LOADERID_FORGE, LoaderHelper.LOADERID_FABRIC,
            LoaderHelper.LOADERID_QUILT
        ], versions);
    }

    public Task<IPaginationHandle<Exhibit>> SearchAsync(string query, Filter filter)
    {
        throw new NotImplementedException();
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