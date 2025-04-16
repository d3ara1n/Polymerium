using System.Net;
using Polymerium.Trident.Models.CurseForgeApi;
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

    private async Task<(IEnumerable<Version>, uint)> GetVersionsAsync(
        uint modId,
        string? version,
        ModLoaderTypeModel? loader,
        int index = 0,
        int count = 50)
    {
        var data = await service.GetModFilesAsync(modId, version, loader, index, count).ConfigureAwait(false);
        // var tasks = data.Data.Select(x => service.GetModFileChangelogAsync(modId, x.Id)).ToArray();
        // await Task.WhenAll(tasks).ConfigureAwait(false);
        // return
        //     (data.Data.Zip(tasks).Select(x => CurseForgeService.ToVersion(x.First, CONVERTER.Convert(x.Second.Result))),
        //      data.Pagination.TotalCount);
        return (data.Data.Select(CurseForgeService.ToVersion), data.Pagination.TotalCount);
    }

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
                                      CurseForgeService.LoaderIdToType(filter.Loader))
                         .ConfigureAwait(false);
        var initial = first.Data.Select(CurseForgeService.ToExhibit);
        return new PaginationHandle<Exhibit>(initial,
                                             50,
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

                {
                    var files = await service
                                     .GetModFilesAsync(modId,
                                                       filter.Version,
                                                       CurseForgeService.LoaderIdToType(filter.Loader),
                                                       count: 1)
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
            var initial = await GetVersionsAsync(modId, filter.Version, CurseForgeService.LoaderIdToType(filter.Loader))
                             .ConfigureAwait(false);
            return new PaginationHandle<Version>(initial.Item1,
                                                 50,
                                                 initial.Item2,
                                                 async index =>
                                                 {
                                                     var data = await GetVersionsAsync(modId,
                                                                        filter.Version,
                                                                        CurseForgeService
                                                                           .LoaderIdToType(filter.Loader),
                                                                        (int)index)
                                                                   .ConfigureAwait(false);
                                                     return data.Item1;
                                                 });
        }

        throw new FormatException("Pid is not well formatted into modId");
    }

    #endregion
}