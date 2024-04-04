using Microsoft.Extensions.Logging;
using Polymerium.Trident.Helpers;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Repositories
{
    public class CurseForgeRepository(ILogger<CurseForgeRepository> logger, IHttpClientFactory factory)
        : IRepository
    {
        public string Label => RepositoryLabels.CURSEFORGE;

        public async Task<Project> QueryAsync(string projectId, CancellationToken token)
        {
            if (uint.TryParse(projectId, out var id))
            {
                return await CurseForgeHelper.GetIntoProjectAsync(logger, factory, id, token);
            }

            throw new ArgumentException("uint needed", nameof(projectId));
        }

        public async Task<Package> ResolveAsync(string projectId, string? versionId, Filter filter,
            CancellationToken token)
        {
            if (uint.TryParse(projectId, out var pid))
            {
                if (versionId != null)
                {
                    if (uint.TryParse(versionId, out var vid))
                    {
                        return await CurseForgeHelper.GetIntoPackageAsync(logger, factory, pid, vid,
                            filter.Version, filter.ModLoader, token);
                    }

                    throw new ArgumentException("uint needed", nameof(versionId));
                }

                return await CurseForgeHelper.GetIntoPackageAsync(logger, factory, pid, null,
                    filter.Version, filter.ModLoader, token);
            }

            throw new ArgumentException("uint needed", nameof(projectId));
        }

        public async Task<IEnumerable<Exhibit>> SearchAsync(string keyword, uint page, uint limit, Filter filter,
            CancellationToken token)
        {
            var kind = filter.Kind ?? ResourceKind.Modpack;
            return (await CurseForgeHelper.SearchProjectsAsync(logger, factory, keyword, kind,
                    filter.Version, filter.ModLoader, page, limit, token))
                .Select(x => new Exhibit(x.Id.ToString(), x.Name, RepositoryLabels.CURSEFORGE, x.Logo?.ThumbnailUrl,
                    kind,
                    string.Join(", ", x.Authors.Select(y => y.Name)), x.Summary, x.DateCreated, x.DateModified,
                    x.DownloadCount));
        }
    }
}