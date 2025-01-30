using Polymerium.Trident.Models.CurseForgeApi;
using Polymerium.Trident.Services;
using Refit;

namespace Polymerium.Trident.Clients;

public interface ICurseForgeClient
{
    [Get("/v1/games/{gameId}/versions")]
    Task<ArrayResponse<GetVersionsResponse>> GetVersionsAsync(uint gameId = CurseForgeService.GAME_ID);

    [Get("/v1/games/{gameId}/version-types")]
    Task<ArrayResponse<GetVersionTypeResponse>> GetVersionTypesAsync(uint gameId = CurseForgeService.GAME_ID);

    [Get("/v1/mods/search")]
    Task<SearchResponse<ModModel>> SearchModsAsync(string searchFilter, uint? classId, string? gameVersion,
        ModLoaderTypeModel? modLoaderType, string sortOrder = "desc", uint index = 0, uint pageSize = 50,
        uint gameId = CurseForgeService.GAME_ID);

    [Get("/v1/mods/{modId}")]
    Task<ObjectResponse<ModModel>> GetModAsync(uint modId);

    [Get("/v1/mods/{modId}/files/{fileId}")]
    Task<ObjectResponse<FileModel>> GetModFileAsync(uint modId, uint fileId);

    [Get("/v1/mods/{modId}/files")]
    Task<ArrayResponse<FileModel>> GetModFilesAsync(uint modId, string? gameVersion, ModLoaderTypeModel? modLoaderType,
        uint? index, uint? pageSize);
}