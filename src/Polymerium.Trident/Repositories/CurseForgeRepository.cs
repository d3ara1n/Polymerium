using Microsoft.Extensions.Logging;
using Polymerium.Trident.Helpers;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Repositories
{
    public class CurseForgeRepository(
        IHttpClientFactory clientFactory,
        ILogger<CurseForgeRepository> logger)
        : IRepository
    {
        public string Label => RepositoryLabels.CURSEFORGE;

        public async Task<Project> QueryAsync(string projectId, CancellationToken token)
        {
            if (uint.TryParse(projectId, out uint id))
            {
                return await CurseForgeHelper.GetIntoProjectAsync(logger, clientFactory, id, token);
            }

            throw new ArgumentException("uint needed", nameof(projectId));
        }

        public async Task<Package> ResolveAsync(string projectId, string? versionId, Filter filter,
            CancellationToken token)
        {
            if (uint.TryParse(projectId, out uint pid))
            {
                if (versionId != null)
                {
                    if (uint.TryParse(versionId, out uint vid))
                    {
                        return await CurseForgeHelper.GetIntoPackageAsync(logger, clientFactory, pid, vid,
                            filter.Version, filter.ModLoader, token);
                    }

                    throw new ArgumentException("uint needed", nameof(versionId));
                }

                return await CurseForgeHelper.GetIntoPackageAsync(logger, clientFactory, pid, null,
                    filter.Version, filter.ModLoader, token);
            }

            throw new ArgumentException("uint needed", nameof(projectId));
        }

        public async Task<IEnumerable<Exhibit>> SearchAsync(string keyword, uint page, uint limit, Filter filter,
            CancellationToken token)
        {
            ResourceKind kind = filter.Kind ?? ResourceKind.Modpack;
            return (await CurseForgeHelper.SearchProjectsAsync(logger, clientFactory, keyword, kind,
                    filter.Version, filter.ModLoader, page, limit, token))
                .Select(x => new Exhibit(x.Id.ToString(), x.Name, RepositoryLabels.CURSEFORGE, x.Logo?.ThumbnailUrl,
                    kind,
                    string.Join(", ", x.Authors.Select(y => y.Name)), x.Summary, x.DateCreated, x.DateModified,
                    x.DownloadCount));
        }
    }
}