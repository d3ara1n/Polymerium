using DotNext;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Helpers;
using Trident.Abstractions.Errors;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Repositories;

public class CurseForgeRepository(
    IHttpClientFactory clientFactory,
    ILogger<CurseForgeRepository> logger)
    : IRepository
{
    public string Label => RepositoryLabels.CURSEFORGE;

    public async Task<Result<Project, ResourceError>> QueryAsync(string projectId, CancellationToken token)
    {
        if (uint.TryParse(projectId, out var id))
        {
            var result = await CurseForgeHelper.GetIntoProjectAsync(logger, clientFactory, id, token);
            if (result != null)
                return new Result<Project, ResourceError>(result);
            return new Result<Project, ResourceError>(ResourceError.NotFound);
        }

        return new Result<Project, ResourceError>(ResourceError.InvalidParameter);
    }

    public async Task<Result<Package, ResourceError>> ResolveAsync(string projectId, string? versionId, Filter filter,
        CancellationToken token)
    {
        if (uint.TryParse(projectId, out var pid))
        {
            var vid = versionId != null && uint.TryParse(versionId, out var r) ? r : (uint?)null;
            var result = await CurseForgeHelper.GetIntoPackageAsync(logger, clientFactory, pid, vid,
                filter.Version, filter.ModLoader, token);
            if (result != null)
                return new Result<Package, ResourceError>(result);
        }

        return new Result<Package, ResourceError>(ResourceError.NotFound);
    }

    public async Task<IEnumerable<Exhibit>> SearchAsync(string keyword, uint page, uint limit, Filter filter,
        CancellationToken token)
    {
        var kind = filter.Kind ?? ResourceKind.Modpack;
        return (await CurseForgeHelper.SearchProjectsAsync(logger, clientFactory, keyword, kind,
                filter.Version, filter.ModLoader, page, limit, token))
            .Select(x => new Exhibit(x.Id.ToString(), x.Name, RepositoryLabels.CURSEFORGE, x.Logo?.ThumbnailUrl, kind,
                string.Join(", ", x.Authors.Select(y => y.Name)), x.Summary, x.DateCreated, x.DateModified,
                x.DownloadCount));
    }
}