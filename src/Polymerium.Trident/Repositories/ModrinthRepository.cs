using System.Net;
using Polymerium.Trident.Clients;
using Polymerium.Trident.Utilities;
using Refit;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Repositories
{
    public class ModrinthRepository(string label, IModrinthClient client) : IRepository
    {
        private const uint PAGE_SIZE = 20;

        #region IRepository Members

        public async Task<RepositoryStatus> CheckStatusAsync()
        {
            var (loadersTask, versionsTask, typesTask) = (client.GetLoadersAsync(), client.GetGameVersionsAsync(),
                                                          client.GetProjectTypesAsync());
            var (loaders, versions, types) = (ModrinthHelper.ToLoaderNames(await loadersTask.ConfigureAwait(false)),
                                              ModrinthHelper.ToVersionNames(await versionsTask.ConfigureAwait(false)),
                                              await typesTask.ConfigureAwait(false));
            var supportedLoaders = loaders
                                  .Select(x => ModrinthHelper.MODLOADER_MAPPINGS.GetValueOrDefault(x))
                                  .Where(x => x != null)
                                  .Select(x => x!)
                                  .ToList();
            var supportedKinds = types
                                .Select(ModrinthHelper.ProjectTypeToKind)
                                .Where(x => x != null)
                                .Select(x => x!.Value)
                                .ToList();
            return new(supportedLoaders, versions.ToList(), supportedKinds);
        }

        public async Task<IPaginationHandle<Exhibit>> SearchAsync(string query, Filter filter)
        {
            var loader = filter.Kind is ResourceKind.Mod ? ModrinthHelper.LoaderIdToName(filter.Loader) : null;
            var first = await client
                             .SearchAsync(query,
                                          ModrinthHelper.BuildFacets(ModrinthHelper.ResourceKindToType(filter.Kind),
                                                                     filter.Version,
                                                                     loader),
                                          limit: PAGE_SIZE)
                             .ConfigureAwait(false);
            var initial = first.Hits.Select(x => ModrinthHelper.ToExhibit(label, x));
            return new PaginationHandle<Exhibit>(initial,
                                                 first.Limit,
                                                 first.TotalHits,
                                                 async (pageIndex, _) =>
                                                 {
                                                     var rv = await client
                                                                   .SearchAsync(query,
                                                                                    ModrinthHelper
                                                                                       .BuildFacets(ModrinthHelper
                                                                                               .ResourceKindToType(filter
                                                                                                   .Kind),
                                                                                            filter.Version,
                                                                                            loader),
                                                                                    offset: pageIndex * first.Limit,
                                                                                    limit: first.Limit)
                                                                   .ConfigureAwait(false);
                                                     var exhibits = rv
                                                                   .Hits.Select(x => ModrinthHelper.ToExhibit(label, x))
                                                                   .ToList();
                                                     return exhibits;
                                                 });
        }

        public async Task<Project> QueryAsync(string? ns, string pid)
        {
            var project = await client.GetProjectAsync(pid).ConfigureAwait(false);
            var team = await client.GetTeamMembersAsync(project.TeamId).ConfigureAwait(false);
            return ModrinthHelper.ToProject(label, project, team.FirstOrDefault());
        }

        public Task<IEnumerable<Project>> QueryBatchAsync(IEnumerable<(string?, string pid)> batch) =>
            throw new NotImplementedException();

        public async Task<Package> ResolveAsync(string? ns, string pid, string? vid, Filter filter)
        {
            try
            {
                var project = await client.GetProjectAsync(pid).ConfigureAwait(false);
                if (vid != null)
                {
                    var (versionTask, membersTask) = (client.GetVersionAsync(vid).ConfigureAwait(false),
                                                      client.GetTeamMembersAsync(project.TeamId).ConfigureAwait(false));
                    var (version, members) = (await versionTask, await membersTask);
                    return ModrinthHelper.ToPackage(label, project, version, members.FirstOrDefault());
                }
                else
                {
                    var (versionTask, membersTask) =
                        (client
                        .GetProjectVersionsAsync(pid,
                                                 null,
                                                 filter.Loader is not null
                                                     ? $"[\"{ModrinthHelper.LoaderIdToName(filter.Loader)}\"]"
                                                     : null)
                        .ConfigureAwait(false), client.GetTeamMembersAsync(project.TeamId).ConfigureAwait(false));
                    var (version, members) = (await versionTask, await membersTask);
                    var found = version.FirstOrDefault(x => filter.Version is null
                                                         || x.GameVersions.Contains(filter.Version));
                    if (found == default)
                    {
                        throw new ResourceNotFoundException($"{pid}/{vid ?? "*"} has not matched version");
                    }

                    return ModrinthHelper.ToPackage(label, project, found, members.FirstOrDefault());
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

        public async Task<string> ReadDescriptionAsync(string? ns, string pid)
        {
            var project = await client.GetProjectAsync(pid).ConfigureAwait(false);
            return project.Description;
        }

        public async Task<string> ReadChangelogAsync(string? ns, string pid, string vid)
        {
            var version = await client.GetVersionAsync(vid).ConfigureAwait(false);
            return version.Changelog ?? string.Empty;
        }

        public async Task<IPaginationHandle<Version>> InspectAsync(string? ns, string pid, Filter filter)
        {
            var project = await client.GetProjectAsync(pid).ConfigureAwait(false);
            var type = project.ProjectTypes.FirstOrDefault();
            var loader = type == ModrinthHelper.RESOURCENAME_MOD ? ModrinthHelper.LoaderIdToName(filter.Loader) : null;
            var first = await client
                             .GetProjectVersionsAsync(pid, null, loader is not null ? $"[\"{loader}\"]" : null)
                             .ConfigureAwait(false);
            var all = first
                     .Where(x => filter.Version is null || x.GameVersions.Contains(filter.Version))
                     .Select(x => ModrinthHelper.ToVersion(label, x))
                     .ToList();
            // Modrinth 的版本无法分页，只能过滤拉取全部之后本地分页
            return new LocalPaginationHandle<Version>(all, PAGE_SIZE);
        }

        #endregion
    }
}
