using Polymerium.Trident.Models.ModrinthApi;
using Refit;

namespace Polymerium.Trident.Clients;

public interface IModrinthClient
{
    [Get("/v2/tag/game_version")]
    Task<IReadOnlyList<GameVersion>> GetGameVersionsAsync();

    [Get("/v3/tag/loader")]
    Task<IReadOnlyList<ModLoader>> GetLoadersAsync();

    [Get("/v2/search")]
    Task<SearchResponse<SearchHit>> SearchAsync(
        string query,
        string facets,
        string? index = null,
        uint offset = 0,
        uint limit = 10);

    [Get("/v3/project/{projectId}")]
    Task GetProjectAsync(string projectId);
}