using System.Net;
using Polymerium.Trident.Clients;
using Polymerium.Trident.Utilities;
using Refit;
using ReverseMarkdown;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Repositories
{
    public class CurseForgeRepository(string label, ICurseForgeClient client) : IRepository
    {
        private const uint PAGE_SIZE = 20;

        private static readonly Converter CONVERTER = new(new() { GithubFlavored = false, SmartHrefHandling = true });

        #region IRepository Members

        public async Task<RepositoryStatus> CheckStatusAsync()
        {
            var raw = await client.GetMinecraftVersionsAsync().ConfigureAwait(false);
            var versions = raw.Data.Select(x => x.VersionString).ToList();
            return new([
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
            var loader = filter.Kind is ResourceKind.Mod ? CurseForgeHelper.LoaderIdToType(filter.Loader) : null;
            var first = await client
                             .SearchModsAsync(query,
                                              CurseForgeHelper.ResourceKindToClassId(filter.Kind),
                                              filter.Version,
                                              loader,
                                              pageSize: PAGE_SIZE)
                             .ConfigureAwait(false);
            var initial = first.Data.Select(x => CurseForgeHelper.ToExhibit(label, x));
            return new PaginationHandle<Exhibit>(initial,
                                                 first.Pagination.PageSize,
                                                 first.Pagination.TotalCount,
                                                 async (pageIndex, _) =>
                                                 {
                                                     var rv = await client
                                                                   .SearchModsAsync(query,
                                                                        CurseForgeHelper
                                                                           .ResourceKindToClassId(filter.Kind),
                                                                        filter.Version,
                                                                        loader,
                                                                        index: pageIndex
                                                                             * first.Pagination.PageSize,
                                                                        pageSize: first.Pagination.PageSize)
                                                                   .ConfigureAwait(false);
                                                     var exhibits = rv
                                                                   .Data.Select(x => CurseForgeHelper.ToExhibit(label,
                                                                                    x))
                                                                   .ToList();
                                                     return exhibits;
                                                 });
        }

        public async Task<Project> QueryAsync(string? _, string pid)
        {
            if (uint.TryParse(pid, out var modId))
            {
                try
                {
                    var mod = await client.GetModAsync(modId).ConfigureAwait(false);
                    return CurseForgeHelper.ToProject(label, mod.Data);
                }
                catch (ApiException ex)
                {
                    if (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new ResourceNotFoundException($"{pid} not found in the repository");
                    }

                    throw;
                }
            }

            throw new FormatException("Pid is not well formatted into modId");
        }

        // POST api.curseforge.com/v1/mods {"modIds": [...]}
        public Task<IEnumerable<Project>> QueryBatchAsync(IEnumerable<(string?, string pid)> batch) =>
            throw new NotImplementedException();

        public async Task<Package> ResolveAsync(string? _, string pid, string? vid, Filter filter)
        {
            if (uint.TryParse(pid, out var modId))
            {
                try
                {
                    // 是否具有 Vid 都应该具有相同次数的 IO Call，以避免其中一个具有更好的性能而受到不公平的待遇
                    // 做不到，LatestFiles 居然并不是最新的，CF 估计有缓存而导致数据迟滞（迟大概三四个月）
                    var mod = (await client.GetModAsync(modId).ConfigureAwait(false)).Data;
                    if (vid is not null)
                    {
                        if (uint.TryParse(vid, out var fileId))
                        {
                            var file = mod.LatestFiles.FirstOrDefault(x => x.Id == fileId);
                            if (file == default)
                            {
                                file = (await client.GetModFileAsync(modId, fileId).ConfigureAwait(false)).Data;
                            }

                            return CurseForgeHelper.ToPackage(label, mod, file);
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
                        var file = (await client
                                         .GetModFilesAsync(modId,
                                                           filter.Version,
                                                           mod.ClassId == CurseForgeHelper.CLASSID_MOD
                                                               ? CurseForgeHelper.LoaderIdToType(filter.Loader)
                                                               : null,
                                                           0,
                                                           1)
                                         .ConfigureAwait(false)).Data.FirstOrDefault();
                        if (file != default)
                        {
                            return CurseForgeHelper.ToPackage(label, mod, file);
                        }

                        throw new ResourceNotFoundException($"{pid}/{vid ?? "*"} has not matched version");
                    }
                }
                catch (ApiException ex)
                {
                    if (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new ResourceNotFoundException($"{pid}/{vid ?? "*"} not found in the repository");
                    }

                    throw;
                }
            }

            throw new FormatException("Pid is not well formatted into modId");
        }

        public async Task<string> ReadDescriptionAsync(string? ns, string pid)
        {
            if (uint.TryParse(pid, out var modId))
            {
                try
                {
                    var html = (await client.GetModDescriptionAsync(modId).ConfigureAwait(false)).Data;
                    return CONVERTER.Convert(html);
                }
                catch (ApiException ex)
                {
                    if (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new ResourceNotFoundException($"{pid} not found in the repository");
                    }

                    throw;
                }
            }

            throw new FormatException("Pid is not well formatted into modId");
        }

        public async Task<string> ReadChangelogAsync(string? ns, string pid, string vid)
        {
            if (uint.TryParse(pid, out var modId) && uint.TryParse(vid, out var fileId))
            {
                try
                {
                    var html = (await client.GetModFileChangelogAsync(modId, fileId).ConfigureAwait(false)).Data;
                    return CONVERTER.Convert(html);
                }
                catch (ApiException ex)
                {
                    if (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new ResourceNotFoundException($"{pid}/{vid} not found in the repository");
                    }

                    throw;
                }
            }

            throw new FormatException("Pid or Vid is not well formatted into modId or fileId");
        }

        public async Task<IPaginationHandle<Version>> InspectAsync(string? _, string pid, Filter filter)
        {
            if (uint.TryParse(pid, out var modId))
            {
                var mod = (await client.GetModAsync(modId).ConfigureAwait(false)).Data;
                var loader = mod.ClassId == CurseForgeHelper.CLASSID_MOD
                                 ? CurseForgeHelper.LoaderIdToType(filter.Loader)
                                 : null;
                var first = await client
                                 .GetModFilesAsync(modId, filter.Version, loader, 0, PAGE_SIZE)
                                 .ConfigureAwait(false);
                var initial = first.Data.Select(x => CurseForgeHelper.ToVersion(label, x));
                return new PaginationHandle<Version>(initial,
                                                     first.Pagination.PageSize,
                                                     first.Pagination.TotalCount,
                                                     async (pageIndex, _) =>
                                                     {
                                                         var rv = await client
                                                                       .GetModFilesAsync(modId,
                                                                            filter.Version,
                                                                            loader,
                                                                            pageIndex * first.Pagination.PageSize,
                                                                            first.Pagination.PageSize)
                                                                       .ConfigureAwait(false);
                                                         return rv.Data.Select(x => CurseForgeHelper
                                                                                  .ToVersion(label, x));
                                                     });
            }

            throw new FormatException("Pid is not well formatted into modId");
        }

        #endregion
    }
}
