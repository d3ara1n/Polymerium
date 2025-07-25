﻿using System.Net;
using Polymerium.Trident.Services;
using Refit;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Trident.Repositories;

public class ModrinthRepository(ModrinthService service) : IRepository
{
    private const uint PAGE_SIZE = 20;

    #region IRepository Members

    public string Label => ModrinthService.LABEL;

    public async Task<RepositoryStatus> CheckStatusAsync()
    {
        var (loadersTask, versionsTask, typesTask) = (service.GetLoadersAsync(), service.GetGameVersionsAsync(),
                                                      service.GetProjectTypesAsync());
        var (loaders, versions, types) = (await loadersTask.ConfigureAwait(false),
                                          await versionsTask.ConfigureAwait(false),
                                          await typesTask.ConfigureAwait(false));
        var supportedLoaders = loaders
                              .Select(x => ModrinthService.MODLOADER_MAPPINGS.GetValueOrDefault(x))
                              .Where(x => x != null)
                              .Select(x => x!)
                              .ToList();
        var supportedKinds = types
                            .Select(ModrinthService.ProjectTypeToKind)
                            .Where(x => x != null)
                            .Select(x => x!.Value)
                            .ToList();
        return new RepositoryStatus(supportedLoaders, versions, supportedKinds);
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

    public async Task<Package> ResolveAsync(string? ns, string pid, string? vid, Filter filter)
    {
        try
        {
            var project = await service.GetProjectAsync(pid).ConfigureAwait(false);
            if (vid != null)
            {
                var (versionTask, membersTask) = (service.GetVersionAsync(vid).ConfigureAwait(false),
                                                  service.GetTeamMembersAsync(project.TeamId).ConfigureAwait(false));
                var (version, members) = (await versionTask, await membersTask);
                return ModrinthService.ToPackage(project, version, members.FirstOrDefault());
            }
            else
            {
                var (versionTask, membersTask) =
                    (service.GetProjectVersionsAsync(pid, null, ModrinthService.LoaderIdToName(filter.Loader)).ConfigureAwait(false),
                     service.GetTeamMembersAsync(project.TeamId).ConfigureAwait(false));
                var (version, members) = (await versionTask, await membersTask);
                return ModrinthService.ToPackage(project,
                                                 version.FirstOrDefault(x => filter.Version is null
                                                                          || x.GameVersions.Contains(filter.Version))
                                              ?? throw new
                                                     ResourceNotFoundException($"{pid}/{vid ?? "*"} has no version found"),
                                                 members.FirstOrDefault());
            }
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                throw new ResourceNotFoundException($"{pid}/{vid ?? "*"} not found in the repository");

            throw;
        }
    }

    public async Task<string> ReadDescriptionAsync(string? ns, string pid)
    {
        var project = await service.GetProjectAsync(pid).ConfigureAwait(false);
        return project.Description;
    }

    public async Task<string> ReadChangelogAsync(string? ns, string pid, string vid)
    {
        var version = await service.GetVersionAsync(vid).ConfigureAwait(false);
        return version.Changelog ?? string.Empty;
    }

    public async Task<IPaginationHandle<Version>> InspectAsync(string? ns, string pid, Filter filter)
    {
        var project = await service.GetProjectAsync(pid).ConfigureAwait(false);
        var type = project.ProjectTypes.FirstOrDefault();
        var loader = type == ModrinthService.RESOURCENAME_MOD ? ModrinthService.LoaderIdToName(filter.Loader) : null;
        var first = await service.GetProjectVersionsAsync(pid, null, loader).ConfigureAwait(false);
        var all = first
                 .Where(x => filter.Version is null || x.GameVersions.Contains(filter.Version))
                 .Select(ModrinthService.ToVersion)
                 .ToList();
        // Modrinth 的版本无法分页，只能过滤拉取全部之后本地分页
        return new LocalPaginationHandle<Version>(all, PAGE_SIZE);
    }

    #endregion
}