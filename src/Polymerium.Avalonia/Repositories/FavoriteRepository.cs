using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Repositories;
using TridentCore.Core.Services;
using TridentCore.Pref;
using Version = TridentCore.Abstractions.Repositories.Resources.Version;

namespace Polymerium.Avalonia.Repositories;

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

    public Task<Project> QueryAsync(ScopedProjectIdentifier id) => throw new NotImplementedException();

    public Task<BatchResolveResult<ScopedProjectIdentifier, Project>> QueryBatchAsync(
        IEnumerable<ScopedProjectIdentifier> batch
    ) =>
        throw new NotImplementedException();

    public Task<Package> ResolveAsync(ScopedPackageIdentifier id, Filter filter) =>
        throw new NotImplementedException();

    public Task<BatchResolveResult<ScopedPackageIdentifier, Package>> ResolveBatchAsync(
        IEnumerable<ScopedPackageIdentifier> batch,
        Filter filter) =>
        throw new NotImplementedException();

    public Task<string> ReadDescriptionAsync(ScopedProjectIdentifier id) => throw new NotImplementedException();

    public Task<string> ReadChangelogAsync(ScopedPackageIdentifier id) => throw new NotImplementedException();

    public Task<IPaginationHandle<Version>> InspectAsync(ScopedProjectIdentifier id, Filter filter) =>
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
            new(favorite.Reference),
            DateTimeHelper.FromPersistedLocalDateTime(favorite.CreatedAt),
            DateTimeHelper.FromPersistedLocalDateTime(favorite.UpdatedAt)
        );
}
