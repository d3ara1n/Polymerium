using Polymerium.Trident.Models.Eternal;
using Polymerium.Trident.Services;
using Refit;

namespace Polymerium.Trident.Clients;

public interface ICurseForgeClient
{
    [Get("/v1/games/{gameId}/versions")]
    Task<ArrayResponse<GetVersionsResponse>> GetVersions(uint gameId = CurseForgeService.GAME_ID);

    [Get("/v1/games/{gameId}/version-types")]
    Task<ArrayResponse<GetVersionTypeResponse>> GetVersionTypes(uint gameId = CurseForgeService.GAME_ID);
}