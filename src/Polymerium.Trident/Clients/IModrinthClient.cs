using Polymerium.Trident.Models.ModrinthApi;
using Refit;

namespace Polymerium.Trident.Clients;

public interface IModrinthClient
{
    [Get("/v2/tag/game_version")]
    Task<IReadOnlyList<GameVersion>> GetGameVersionsAsync();

    [Get("/v3/tag/loader")]
    Task<IReadOnlyList<ModLoader>> GetLoadersAsync();

    [Get("/v3/search")]
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, string facets);

    [Get("/v3/project/{projectId}")]
    Task GetProjectAsync(string projectId);
}