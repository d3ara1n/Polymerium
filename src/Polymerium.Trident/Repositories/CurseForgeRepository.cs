using Polymerium.Trident.Services;
using Refit;
using System.Net;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Repositories;

public class CurseForgeRepository(CurseForgeService service) : IRepository
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

    public Task<Project> QueryAsync(string? _, string pid) => throw new NotImplementedException();

    public async Task<Package> ResolveAsync(string? _, string pid, string? vid, Filter filter)
    {
        if (uint.TryParse(pid, out var modId))
            try
            {
                var mod = await service.GetModAsync(modId);
                if (vid is not null)
                {
                    if (uint.TryParse(vid, out var fileId))
                    {
                        var file = await service.GetModFileAsync(modId, fileId);
                        return service.ToPackage(mod, file);
                    }

                    throw new FormatException("Vid is not well formatted into fileId");
                }

                {
                    var files = await service.GetModFilesAsync(modId, filter.Version,
                        service.LoaderIdToType(filter.Loader),
                        count: 1);
                    var file = files.Data.FirstOrDefault();
                    if (file is not null)
                        return service.ToPackage(mod, file);
                    throw new ResourceNotFoundException($"{pid}/{vid ?? "*"} has not matched version");
                }
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    throw new ResourceNotFoundException($"{pid}/{vid ?? "*"} not found in the repository");
                throw;
            }

        throw new FormatException("Pid is not well formatted into modId");
    }

    public async Task<IPaginationHandle<Version>> InspectAsync(string? _, string pid, Filter filter)
    {
        if (uint.TryParse(pid, out var modId))
        {
            var first = await service.GetModFilesAsync(modId, filter.Version, service.LoaderIdToType(filter.Loader));
            var initial = first.Data.Select(service.ToVersion);
            return new PaginationHandle<Version>(initial, 50, first.Pagination.TotalCount, async index =>
            {
                var rv = await service.GetModFilesAsync(modId, filter.Version, service.LoaderIdToType(filter.Loader),
                    (int)index);
                return first.Data.Select(service.ToVersion);
            });
        }

        throw new FormatException("Pid is not well formatted into modId");
    }
}