using Polymerium.Trident.Models.ModrinthApi;
using Refit;

namespace Polymerium.Trident.Clients;

public interface IModrinthClient
{
    [Get("/v3/search")]
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, string facets);

    [Get("/v3/project/{projectId}")]
    Task GetProjectAsync(string projectId);
}