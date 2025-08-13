using System.Net;
using Polymerium.Trident.Services;
using Refit;
using ReverseMarkdown;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using FileInfo = Polymerium.Trident.Models.CurseForgeApi.FileInfo;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Repositories;

public class CurseForgeRepository(CurseForgeService service) : IRepository
{
    private const uint PAGE_SIZE = 20;
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
                                        ResourceKind.ShaderPack,
                                        ResourceKind.World,
                                        ResourceKind.DataPack
                                    ]);
    }

    public async Task<IPaginationHandle<Exhibit>> SearchAsync(string query, Filter filter)
    {
        var loader = filter.Kind is ResourceKind.Mod ? CurseForgeService.LoaderIdToType(filter.Loader) : null;
        var first = await service
                         .SearchAsync(query,
                                      CurseForgeService.ResourceKindToClassId(filter.Kind),
                                      filter.Version,
                                      loader,
                                      pageSize: PAGE_SIZE)
                         .ConfigureAwait(false);
        var initial = first.Data.Select(CurseForgeService.ToExhibit);
        return new PaginationHandle<Exhibit>(initial,
                                             first.Pagination.PageSize,
                                             first.Pagination.TotalCount,
                                             async (pageIndex, _) =>
                                             {
                                                 var rv = await service
                                                               .SearchAsync(query,
                                                                            CurseForgeService
                                                                               .ResourceKindToClassId(filter.Kind),
                                                                            filter.Version,
                                                                            loader,
                                                                            pageIndex * first.Pagination.PageSize,
                                                                            first.Pagination.PageSize)
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

    // POST api.curseforge.com/v1/mods {"modIds": [...]}
    public Task<IEnumerable<Project>> QueryBatchAsync(IEnumerable<(string?, string pid)> batch) =>
        throw new NotImplementedException();

    public async Task<Package> ResolveAsync(string? _, string pid, string? vid, Filter filter)
    {
        if (uint.TryParse(pid, out var modId))
            try
            {
                // 是否具有 Vid 都应该具有相同次数的 IO Call，以避免其中一个具有更好的性能而受到不公平的待遇
                // 做不到，LatestFiles 居然并不是最新的，CF 估计有缓存而导致数据迟滞（迟大概三四个月）
                var mod = await service.GetModAsync(modId).ConfigureAwait(false);
                if (vid is not null)
                {
                    if (uint.TryParse(vid, out var fileId))
                    {
                        var file = mod.LatestFiles.FirstOrDefault(x => x.Id == fileId);
                        if (file == default(FileInfo))
                        {
                            file = await service.GetModFileAsync(modId, fileId).ConfigureAwait(false);
                        }

                        return CurseForgeService.ToPackage(mod, file);
                    }

                    throw new FormatException("Vid is not well formatted into fileId");
                }

                {
                    // var loaderNick = CurseForgeService.LoaderIdToName(filter.Loader);
                    // // GameVersion 是游戏版本，GameVersionName 是游戏版本或加载器版本
                    // // 如果加载器过滤无效或不存在、如果非模组都将短路加载器判断
                    // // LatestFiles 基本上是各个主流版本或加载器的最新版本集合，命中率较高，除了有些会把版本 1.21.1 标记为 1.21 的模组
                    // var found = mod.LatestFiles.FirstOrDefault(x => x.SortableGameVersions.Any(y => y.GameVersion
                    //                                                               == filter.Version)
                    //                                                  && (loaderNick == null
                    //                                                   || mod.ClassId != CurseForgeService.CLASSID_MOD
                    //                                                   || x.SortableGameVersions.Any(y => y.GameVersionName
                    //                                                                == loaderNick)));
                    var file = (await service
                                     .GetModFilesAsync(modId,
                                                       filter.Version,
                                                       mod.ClassId == CurseForgeService.CLASSID_MOD
                                                           ? CurseForgeService.LoaderIdToType(filter.Loader)
                                                           : null,
                                                       pageSize: 1)
                                     .ConfigureAwait(false)).Data.FirstOrDefault();
                    if (file != default(FileInfo))
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

    public async Task<string> ReadDescriptionAsync(string? ns, string pid)
    {
        if (uint.TryParse(pid, out var modId))
            try
            {
                var html = await service.GetModDescriptionAsync(modId).ConfigureAwait(false);
                return CONVERTER.Convert(html);
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    throw new ResourceNotFoundException($"{pid} not found in the repository");

                throw;
            }

        throw new FormatException("Pid is not well formatted into modId");
    }

    public async Task<string> ReadChangelogAsync(string? ns, string pid, string vid)
    {
        if (uint.TryParse(pid, out var modId) && uint.TryParse(vid, out var fileId))
            try
            {
                var html = await service.GetModFileChangelogAsync(modId, fileId).ConfigureAwait(false);
                return CONVERTER.Convert(html);
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    throw new ResourceNotFoundException($"{pid}/{vid} not found in the repository");

                throw;
            }

        throw new FormatException("Pid or Vid is not well formatted into modId or fileId");
    }

    public async Task<IPaginationHandle<Version>> InspectAsync(string? _, string pid, Filter filter)
    {
        if (uint.TryParse(pid, out var modId))
        {
            var mod = await service.GetModAsync(modId).ConfigureAwait(false);
            var loader = mod.ClassId == CurseForgeService.CLASSID_MOD
                             ? CurseForgeService.LoaderIdToType(filter.Loader)
                             : null;
            var first = await service
                             .GetModFilesAsync(modId, filter.Version, loader, pageSize: PAGE_SIZE)
                             .ConfigureAwait(false);
            var initial = first.Data.Select(CurseForgeService.ToVersion);
            return new PaginationHandle<Version>(initial,
                                                 first.Pagination.PageSize,
                                                 first.Pagination.TotalCount,
                                                 async (pageIndex, _) =>
                                                 {
                                                     var rv = await service
                                                                   .GetModFilesAsync(modId,
                                                                        filter.Version,
                                                                        loader,
                                                                        pageIndex * first.Pagination.PageSize,
                                                                        first.Pagination.PageSize)
                                                                   .ConfigureAwait(false);
                                                     return rv.Data.Select(CurseForgeService.ToVersion);
                                                 });
        }

        throw new FormatException("Pid is not well formatted into modId");
    }

    #endregion
}