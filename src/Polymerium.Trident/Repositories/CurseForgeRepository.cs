using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Helpers;
using System.Text.Json;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Repositories
{
    public class CurseForgeRepository : IRepository
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly JsonSerializerOptions _options;
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;

        public CurseForgeRepository(IHttpClientFactory clientFactory, JsonSerializerOptions options, ILogger<CurseForgeRepository> logger, IMemoryCache cache)
        {
            _clientFactory = clientFactory;
            _options = options;
            _logger = logger;
            _cache = cache;
        }

        public string Label => RepositoryLabels.CURSEFORGE;

        public Project Query(string projectId)
        {
            throw new NotImplementedException();
        }

        public Package Resolve(string projectId, string? versionId, Filter filter)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Exhibit>> SearchAsync(string keyword, uint page, uint limit, Filter filter, CancellationToken token)
        {
            var kind = filter.Kind ?? ResourceKind.Modpack;
            return (await CurseForgeHelper.SearchProjectsAsync(_logger, _clientFactory, _cache, keyword, kind, filter.Version, filter.ModLoader, page, limit, token))
                .Select(x => new Exhibit(x.Id.ToString(), x.Name, x.Logo?.ThumbnailUrl, kind, string.Join(", ", x.Authors.Select(y => y.Name)), x.Summary, x.DateCreated, x.DateModified, x.DownloadCount));
        }
    }
}
