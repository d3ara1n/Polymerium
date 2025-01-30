using Microsoft.Extensions.Caching.Memory;
using Polymerium.Trident.Services;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Repositories;

public class CurseForgeRepository(CurseForgeService service, IMemoryCache cache) : IRepository
{
    public string Label => CurseForgeService.LABEL;

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
        var initial = first.Data.Select(service.ToExhibit);
        return new PaginationHandle<Exhibit>(initial, 50, first.Pagination.TotalCount, async index =>
        {
            var rv = await service.SearchAsync(query, service.ResourceKindToClassId(filter.Kind), filter.Version,
                service.LoaderIdToType(filter.Loader), index);
            var exhibits = rv.Data.Select(service.ToExhibit).ToList();
            return exhibits;
        });
    }

    public Task<Project> QueryAsync(string? _, string pid)
    {
        throw new NotImplementedException();
    }

    public async Task<Package> ResolveAsync(string? _, string pid, string? vid, Filter filter)
    {
        if (uint.TryParse(pid, out var modId))
        {
            var mod = await service.GetModAsync(modId);
            if (vid is not null)
            {
                if (uint.TryParse(vid, out var fileId))
                {
                    var file = await service.GetModFileAsync(modId, fileId);
                    return service.ToPackage(mod, file);
                }
                else
                {
                    throw new FormatException("Vid is not well formatted into fileId");
                }
            }
            else
            {
                var files = await service.GetModFilesAsync(modId, filter.Version, service.LoaderIdToType(filter.Loader),
                    3);
                var file = files.FirstOrDefault();
                if (file is not null)
                    return service.ToPackage(mod, file);
                else throw new ResourceNotFoundException($"{pid}/{vid ?? "*"} has not matched version");
            }
        }
        else
        {
            throw new FormatException("Pid is not well formatted into modId");
        }
    }

    public Task<IPaginationHandle<Version>> InspectAsync(string? _, string pid, Filter filter)
    {
        throw new NotImplementedException();
    }
}