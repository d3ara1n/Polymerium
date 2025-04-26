using System.Net;
using Polymerium.Trident.Services;
using Refit;
using ReverseMarkdown;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Repositories;

public class CurseForgeRepository(CurseForgeService service) : IRepository
{
    private static readonly Converter CONVERTER = new(new Config { GithubFlavored = false, SmartHrefHandling = true });


    #region IRepository Members

    public string Label => CurseForgeService.LABEL;

    public async Task<RepositoryStatus> CheckStatusAsync()
    {
        var versions = await service.GetGameVersionsAsync().ConfigureAwait(false);
        return new RepositoryStatus([
                                        LoaderHelper.LOADERID_NEOFORGE,
                                        LoaderHelper.LOADERID_FORGE,
                                        LoaderHelper.LOADERID_FABRIC,
                                        LoaderHelper.LOADERID_QUILT
                                    ],
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
        var first = await service
                         .SearchAsync(query,
                                      CurseForgeService.ResourceKindToClassId(filter.Kind),
                                      filter.Version,
                                      filter.Kind is ResourceKind.Mod
                                          ? CurseForgeService.LoaderIdToType(filter.Loader)
                                          : null)
                         .ConfigureAwait(false);
        var initial = first.Data.Select(CurseForgeService.ToExhibit);
        return new PaginationHandle<Exhibit>(initial,
                                             first.Pagination.PageSize,
                                             first.Pagination.TotalCount,
                                             async pageIndex =>
                                             {
                                                 var rv = await service
                                                               .SearchAsync(query,
                                                                            CurseForgeService
                                                                               .ResourceKindToClassId(filter.Kind),
                                                                            filter.Version,
                                                                            CurseForgeService
                                                                               .LoaderIdToType(filter.Loader),
                                                                            pageIndex * first.Pagination.PageSize)
                                                               .ConfigureAwait(false);
                                                 var exhibits = rv.Data.Select(CurseForgeService.ToExhibit).ToList();
                                                 return exhibits;
                                             });
    }

    public async Task<Project> QueryAsync(string? _, string pid)
    {
        if (uint.TryParse(pid, out var modId))
            try
            {
                var mod = await service.GetModAsync(modId).ConfigureAwait(false);
                return CurseForgeService.ToProject(mod);
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    throw new ResourceNotFoundException($"{pid} not found in the repository");

                throw;
            }

        throw new FormatException("Pid is not well formatted into modId");
    }

    public async Task<Package> ResolveAsync(string? _, string pid, string? vid, Filter filter)
    {
        if (uint.TryParse(pid, out var modId))
            try
            {
                var mod = await service.GetModAsync(modId).ConfigureAwait(false);
                if (vid is not null)
                {
                    if (uint.TryParse(vid, out var fileId))
                    {
                        var file = await service.GetModFileAsync(modId, fileId).ConfigureAwait(false);
                        return CurseForgeService.ToPackage(mod, file);
                    }

                    throw new FormatException("Vid is not well formatted into fileId");
                }
                else
                {
                    var files = await service
                                     .GetModFilesAsync(modId,
                                                       filter.Version,
                                                       mod.ClassId == CurseForgeService.CLASSID_MOD
                                                           ? CurseForgeService.LoaderIdToType(filter.Loader)
                                                           : null)
                                     .ConfigureAwait(false);
                    var file = files.Data.FirstOrDefault();
                    if (file is not null)
                        return CurseForgeService.ToPackage(mod, file);

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
            var mod = await service.GetModAsync(modId).ConfigureAwait(false);
            var first = await service
                             .GetModFilesAsync(modId,
                                               filter.Version,
                                               mod.ClassId == CurseForgeService.CLASSID_MOD
                                                   ? CurseForgeService.LoaderIdToType(filter.Loader)
                                                   : null)
                             .ConfigureAwait(false);
            var initial = first.Data.Select(CurseForgeService.ToVersion);
            return new PaginationHandle<Version>(initial,
                                                 first.Pagination.PageSize,
                                                 first.Pagination.TotalCount,
                                                 async pageIndex =>
                                                 {
                                                     var data = await service
                                                                     .GetModFilesAsync(modId,
                                                                          filter.Version,
                                                                          CurseForgeService
                                                                             .LoaderIdToType(filter.Loader),
                                                                          pageIndex * first.Pagination.PageSize)
                                                                     .ConfigureAwait(false);
                                                     return data.Data.Select(CurseForgeService.ToVersion);
                                                 });
        }

        throw new FormatException("Pid is not well formatted into modId");
    }

    #endregion
}