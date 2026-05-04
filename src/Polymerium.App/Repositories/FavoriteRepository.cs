using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Services;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Repositories;
using TridentCore.Core.Services;
using TridentCore.Purl;
using Version = TridentCore.Abstractions.Repositories.Resources.Version;

namespace Polymerium.App.Repositories;

public class FavoriteRepository(
    PersistenceService persistenceService,
    PrismLauncherService prismLauncherService) : IRepository
{
    private const uint PAGE_SIZE = 20;

    #region IRepository Members

    public async Task<RepositoryStatus> CheckStatusAsync()
    {
        List<string> loaders =
        [
            LoaderHelper.LOADERID_NEOFORGE,
            LoaderHelper.LOADERID_FORGE,
            LoaderHelper.LOADERID_FABRIC,
            LoaderHelper.LOADERID_QUILT,
        ];

        var index = await prismLauncherService.GetMinecraftVersionsAsync(CancellationToken.None).ConfigureAwait(false);
        var versions = index.Versions.Select(x => x.Version).ToList();

        List<ResourceKind> kinds =
        [
            ResourceKind.Modpack,
            ResourceKind.Mod,
            ResourceKind.ResourcePack,
            ResourceKind.ShaderPack,
            ResourceKind.DataPack,
            ResourceKind.World,
            ResourceKind.Unknown
        ];
        return new(loaders, versions, kinds);
    }

    public Task<IPaginationHandle<Exhibit>> SearchAsync(string query, Filter filter)
    {
        var first = persistenceService.SearchFavoriteProjects(
            query,
            filter,
            0,
            PAGE_SIZE,
            out var totalCount
        );
        var initial = first.Select(ToExhibit).ToList();

        return Task.FromResult<IPaginationHandle<Exhibit>>(
            new PaginationHandle<Exhibit>(
                initial,
                PAGE_SIZE,
                (uint)totalCount,
                (pageIndex, _) =>
                {
                    var favorites = persistenceService.SearchFavoriteProjects(
                        query,
                        filter,
                        pageIndex,
                        PAGE_SIZE,
                        out var _
                    );
                    return Task.FromResult(favorites.Select(ToExhibit));
                }
            )
        );
    }

    public Task<Package> IdentifyAsync(ReadOnlyMemory<byte> content) => throw new NotImplementedException();

    public Task<Project> QueryAsync(string? ns, string pid) => throw new NotImplementedException();

    public Task<IReadOnlyList<Project>> QueryBatchAsync(IEnumerable<(string?, string pid)> batch) =>
        throw new NotImplementedException();

    public Task<Package> ResolveAsync(string? ns, string pid, string? vid, Filter filter) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<(ScopedPackageIdentifier, Package)>> ResolveBatchAsync(
        IEnumerable<ScopedPackageIdentifier> batch,
        Filter filter) =>
        throw new NotImplementedException();

    public Task<string> ReadDescriptionAsync(string? ns, string pid) => throw new NotImplementedException();

    public Task<string> ReadChangelogAsync(string? ns, string pid, string vid) => throw new NotImplementedException();

    public Task<IPaginationHandle<Version>> InspectAsync(string? ns, string pid, Filter filter) =>
        throw new NotImplementedException();

    #endregion

    private static Exhibit ToExhibit(PersistenceService.FavoriteProject favorite) =>
        new(
            favorite.Label,
            PersistenceService.NormalizeFavoriteNamespace(favorite.Namespace),
            favorite.ProjectId,
            favorite.ProjectName,
            string.IsNullOrWhiteSpace(favorite.Thumbnail) ? null : new Uri(favorite.Thumbnail),
            favorite.Author,
            favorite.Summary,
            favorite.Kind,
            favorite.DownloadCount,
            PersistenceService.DeserializeFavoriteTags(favorite.Tags),
            new Uri(favorite.Reference),
            new DateTimeOffset(favorite.CreatedAt),
            new DateTimeOffset(favorite.UpdatedAt)
        );
}
